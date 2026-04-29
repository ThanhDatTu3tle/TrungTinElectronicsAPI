using Microsoft.Data.SqlClient;
using System.Data;
using TrungTinElectronicsAPI.Services;

namespace TrungTinElectronics.Jobs;

public class CancelExpiredOrdersJob
{
    private readonly string _connectionString;
    private readonly ILogger<CancelExpiredOrdersJob> _logger;
    private readonly INotificationService _notificationService;


    public CancelExpiredOrdersJob(
        IConfiguration config,
        ILogger<CancelExpiredOrdersJob> logger,
        INotificationService notificationService)
    {
        _connectionString = config.GetConnectionString("DefaultConnection")!;
        _logger = logger;
        _notificationService = notificationService;
    }

    public async Task ExecuteAsync()
    {
        _logger.LogInformation("Cron chạy lúc {Time}: check đơn timeout", DateTime.Now);

        // Lấy danh sách đơn hết hạn — batch 100, tránh query nặng
        const string selectSql = """
            SELECT TOP 100 OrderID
            FROM dbo.Orders
            WHERE Status = 'pending_payment'
              AND ExpiredAt < GETDATE()
            """;

        var expiredOrderIds = new List<int>();

        await using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync();

        await using (var cmd = new SqlCommand(selectSql, conn))
        await using (var reader = await cmd.ExecuteReaderAsync())
        {
            while (await reader.ReadAsync())
                expiredOrderIds.Add(reader.GetInt32(0));
        }

        if (!expiredOrderIds.Any())
        {
            _logger.LogInformation("Không có đơn nào timeout");
            return;
        }

        _logger.LogInformation("Tìm thấy {Count} đơn timeout, tiến hành hủy...", expiredOrderIds.Count);

        foreach (var orderId in expiredOrderIds)
        {
            await CancelOrderAsync(conn, orderId);
        }

        _logger.LogInformation("Hoàn tất hủy {Count} đơn", expiredOrderIds.Count);
    }

    private async Task CancelOrderAsync(SqlConnection conn, int orderId)
    {
        // Dùng SP riêng — gộp hoàn kho + hủy đơn trong 1 transaction
        await using var cmd = new SqlCommand("sp_CancelExpiredOrder", conn)
        {
            CommandType = CommandType.StoredProcedure
        };
        cmd.Parameters.AddWithValue("@OrderID", orderId);

        var errorParam = new SqlParameter("@ErrorMessage", SqlDbType.NVarChar, 500)
        { Direction = ParameterDirection.Output };
        cmd.Parameters.Add(errorParam);

        await cmd.ExecuteNonQueryAsync();

        var error = errorParam.Value as string;
        if (!string.IsNullOrEmpty(error))
            _logger.LogWarning("Hủy đơn {OrderID} lỗi: {Error}", orderId, error);
        else
        {
            _logger.LogInformation("Đã hủy đơn {OrderID}", orderId);
            await _notificationService.CreateCancelNotificationAsync(orderId);
        }
    }
}