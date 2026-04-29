using Microsoft.Data.SqlClient;
using System.Data;
using TrungTinElectronics.Models;
using TrungTinElectronicsAPI.Models;
using TrungTinElectronicsAPI.Services;

namespace TrungTinElectronics.Jobs;

public class PaymentCallbackWorker : BackgroundService
{
    private const string QUEUE_NAME = "payment:callback";
    private const int MAX_RETRY = 3;

    private readonly RedisQueueService _queue;
    private readonly IConfiguration _config;
    private readonly ILogger<PaymentCallbackWorker> _logger;
    private readonly IServiceScopeFactory _scopeFactory;

    public PaymentCallbackWorker(
        RedisQueueService queue,
        IConfiguration config,
        ILogger<PaymentCallbackWorker> logger,
        IServiceScopeFactory scopeFactory)
    {
        _queue = queue;
        _config = config;
        _logger = logger;
        _scopeFactory = scopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("PaymentCallbackWorker started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var job = await _queue.DequeueAsync<PaymentCallbackJob>(QUEUE_NAME);

                if (job is null)
                {
                    // Queue trống — chờ 1 giây rồi check lại
                    await Task.Delay(1000, stoppingToken);
                    continue;
                }

                await ProcessPaymentAsync(job);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "PaymentCallbackWorker error");
                await Task.Delay(2000, stoppingToken);
            }
        }
    }

    private async Task ProcessPaymentAsync(PaymentCallbackJob job)
    {
        _logger.LogInformation("Processing payment job: OrderID={OrderID}, ResponseCode={Code}",
            job.OrderID, job.ResponseCode);

        // Idempotency check — tránh xử lý 2 lần
        var lockKey = $"payment:lock:{job.OrderID}";
        var locked = await _queue.AcquireLockAsync(lockKey, TimeSpan.FromMinutes(5));

        if (!locked)
        {
            _logger.LogWarning("OrderID {OrderID} đã xử lý, skip", job.OrderID);
            return;
        }

        try
        {
            var connStr = _config.GetConnectionString("DefaultConnection")!;
            await using var conn = new SqlConnection(connStr);
            await conn.OpenAsync();

            // Kiểm tra đơn hàng
            var order = await GetOrderAsync(conn, job.OrderID);
            if (order is null || order.Value.Status != "pending_payment")
            {
                _logger.LogWarning("OrderID {OrderID} không hợp lệ hoặc đã xử lý", job.OrderID);
                return;
            }

            if (job.ResponseCode == "00")
                await HandleSuccessAsync(conn, job);
            else
                await HandleFailureAsync(conn, job);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi xử lý payment OrderID={OrderID}", job.OrderID);

            // Retry nếu chưa quá MAX_RETRY
            if (job.AttemptCount < MAX_RETRY)
            {
                job.AttemptCount++;
                await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, job.AttemptCount))); // 2s, 4s, 8s
                await _queue.EnqueueAsync(QUEUE_NAME, job);
                _logger.LogInformation("Retry lần {Attempt} cho OrderID={OrderID}", job.AttemptCount, job.OrderID);
            }
            else
            {
                _logger.LogError("OrderID {OrderID} failed sau {Max} lần retry!", job.OrderID, MAX_RETRY);
                // TODO: alert admin
            }
        }
    }

    private async Task HandleSuccessAsync(SqlConnection conn, PaymentCallbackJob job)
    {
        await using var transaction = conn.BeginTransaction();
        try
        {
            // 1. Update order status = paid
            await using var updateOrder = new SqlCommand("""
                UPDATE dbo.Orders
                SET Status = 'paid', PaidAt = GETDATE(), UpdatedAt = GETDATE()
                WHERE OrderID = @OrderID AND Status = 'pending_payment'
                """, conn, transaction);
            updateOrder.Parameters.AddWithValue("@OrderID", job.OrderID);
            await updateOrder.ExecuteNonQueryAsync();

            // 2. Lưu transaction log
            await using var insertTx = new SqlCommand("""
                INSERT INTO dbo.Transactions (OrderID, ExternalTransId, Amount, Provider, Status, CreatedAt)
                VALUES (@OrderID, @ExternalTransId, @Amount, @Provider, 'success', GETDATE())
                """, conn, transaction);
            insertTx.Parameters.AddWithValue("@OrderID", job.OrderID);
            insertTx.Parameters.AddWithValue("@ExternalTransId", job.TransactionId);
            insertTx.Parameters.AddWithValue("@Amount", job.Amount);
            insertTx.Parameters.AddWithValue("@Provider", job.Provider);
            await insertTx.ExecuteNonQueryAsync();

            await transaction.CommitAsync();

            _logger.LogInformation("OrderID {OrderID} thanh toán thành công!", job.OrderID);

            // 3. Invalidate revenue cache
            await _queue.DeleteCacheAsync("revenue:summary:month");
            await _queue.DeleteCacheAsync($"revenue:daily:{DateTime.Now:yyyy-MM-dd}");

            // 4. Tạo notification cho admin
            using var scope = _scopeFactory.CreateScope();
            var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();
            await notificationService.CreatePaymentNotificationAsync(
                orderId: job.OrderID,
                amount: job.Amount
            );
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    private async Task HandleFailureAsync(SqlConnection conn, PaymentCallbackJob job)
    {
        await using var cmd = new SqlCommand("""
            UPDATE dbo.Orders
            SET Status = 'payment_failed', UpdatedAt = GETDATE()
            WHERE OrderID = @OrderID AND Status = 'pending_payment'
            """, conn);
        cmd.Parameters.AddWithValue("@OrderID", job.OrderID);
        await cmd.ExecuteNonQueryAsync();

        _logger.LogWarning("OrderID {OrderID} thanh toán thất bại, ResponseCode={Code}",
            job.OrderID, job.ResponseCode);
    }

    private async Task<(int OrderID, string Status)?> GetOrderAsync(SqlConnection conn, int orderId)
    {
        await using var cmd = new SqlCommand(
            "SELECT OrderID, Status FROM dbo.Orders WHERE OrderID = @OrderID", conn);
        cmd.Parameters.AddWithValue("@OrderID", orderId);

        await using var reader = await cmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync()) return null;

        return (reader.GetInt32(0), reader.GetString(1));
    }
}