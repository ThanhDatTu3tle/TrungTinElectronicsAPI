using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using TrungTinElectronics.Models;
using TrungTinElectronics.Repositories;
using TrungTinElectronics.Services;
using TrungTinElectronicsAPI.Services;

namespace TrungTinElectronics.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrderController : ControllerBase
{
    private readonly OrderService _orderService;
    private readonly OrderRepository _orderRepo;
    private readonly RedisQueueService _redisQueue;
    private readonly IConfiguration _config;

    public OrderController(OrderService orderService, OrderRepository orderRepo, RedisQueueService redisQueue, IConfiguration config)
    {
        _orderService = orderService;
        _orderRepo = orderRepo;
        _redisQueue = redisQueue;
        _config = config;
    }

    // POST /api/order/create
    [HttpPost("create")]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest request)
    {
        var result = await _orderService.CreateOrderAsync(request);

        if (!result.Success)
            return BadRequest(new { result.ErrorMessage });

        return Ok(result);
    }

    // GET /api/order/{id}
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetOrderDetail(int id)
    {
        var order = await _orderRepo.GetOrderDetailAsync(id);

        if (order is null)
            return NotFound(new { ErrorMessage = "Không tìm thấy đơn hàng" });

        return Ok(order);
    }

    // POST /api/order/payment-callback — giả lập callback từ VNPAY
    [HttpPost("payment-callback")]
    public async Task<IActionResult> PaymentCallback([FromBody] PaymentCallbackJob job)
    {
        // Trong thực tế: verify chữ ký VNPAY tại đây trước
        await _redisQueue.EnqueueAsync("payment:callback", job);
        return Ok(new { message = "Callback received, processing..." });
    }

    // GET /api/order/{id}/status — check trạng thái đơn
    [HttpGet("{id:int}/status")]
    public async Task<IActionResult> GetOrderStatus(int id)
    {
        const string sql = """
        SELECT OrderID, Status, PaidAt, UpdatedAt
        FROM dbo.Orders WHERE OrderID = @OrderID
        """;

        await using var conn = new SqlConnection(
            _config.GetConnectionString("DefaultConnection"));
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@OrderID", id);

        await conn.OpenAsync();
        await using var reader = await cmd.ExecuteReaderAsync();

        if (!await reader.ReadAsync())
            return NotFound();

        return Ok(new
        {
            OrderID = reader.GetInt32(0),
            Status = reader.GetString(1),
            PaidAt = reader.IsDBNull(2) ? (DateTime?)null : reader.GetDateTime(2),
            UpdatedAt = reader.GetDateTime(3),
        });
    }

    // GET /api/order?status=paid&page=1&pageSize=10
    [HttpGet]
    public async Task<IActionResult> GetAllOrders(
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var (orders, totalCount) = await _orderService.GetAllOrdersAsync(status, page, pageSize);
        return Ok(new
        {
            data = orders,
            totalCount,
            page,
            pageSize,
            totalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        });
    }
}