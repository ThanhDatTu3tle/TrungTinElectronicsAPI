using System.Data;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using TrungTinElectronics.Models;

namespace TrungTinElectronics.Repositories;

public class OrderRepository
{
    private readonly string _connectionString;

    public OrderRepository(IConfiguration config)
    {
        _connectionString = config.GetConnectionString("DefaultConnection")!;
    }

    public async Task<(int OrderID, string? ErrorMessage)> CreateOrderAsync(
        int userId, string? note, List<CartItemRequest> items)
    {
        var itemsJson = JsonSerializer.Serialize(
            items.Select(i => new { i.ProductId, i.Quantity })
        );

        await using var conn = new SqlConnection(_connectionString);
        await using var cmd = new SqlCommand("sp_CreateOrder", conn)
        {
            CommandType = CommandType.StoredProcedure
        };

        cmd.Parameters.AddWithValue("@UserID", userId);
        cmd.Parameters.AddWithValue("@Note", (object?)note ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Items", itemsJson);

        var orderIdParam = new SqlParameter("@OrderID", SqlDbType.Int)
        { Direction = ParameterDirection.Output };
        var errorParam = new SqlParameter("@ErrorMessage", SqlDbType.NVarChar, 500)
        { Direction = ParameterDirection.Output };

        cmd.Parameters.Add(orderIdParam);
        cmd.Parameters.Add(errorParam);

        await conn.OpenAsync();
        await cmd.ExecuteNonQueryAsync();

        return (
            (int)orderIdParam.Value,
            errorParam.Value as string
        );
    }

    public async Task<OrderDetailResponse?> GetOrderDetailAsync(int orderId)
    {
        const string sql = """
            SELECT
                o.OrderID,
                o.Status,
                o.TotalAmount,
                o.ExpiredAt,
                o.CreatedAt,
                oi.ProductId,
                p.ProductName,
                oi.Quantity,
                oi.UnitPrice,
                oi.DiscountPrice,
                oi.Subtotal
            FROM dbo.Orders o
            INNER JOIN dbo.Order_Items oi ON o.OrderID    = oi.OrderID
            INNER JOIN dbo.Product     p  ON oi.ProductId = p.ProductId
            WHERE o.OrderID = @OrderID
            """;

        await using var conn = new SqlConnection(_connectionString);
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@OrderID", orderId);

        await conn.OpenAsync();
        await using var reader = await cmd.ExecuteReaderAsync();

        OrderDetailResponse? order = null;

        while (await reader.ReadAsync())
        {
            order ??= new OrderDetailResponse
            {
                OrderID = reader.GetInt32(0),
                Status = reader.GetString(1),
                TotalAmount = reader.GetDecimal(2),
                ExpiredAt = reader.GetDateTime(3),
                CreatedAt = reader.GetDateTime(4),
            };

            order.Items.Add(new OrderItemDetail
            {
                ProductId = reader.GetInt32(5),
                ProductName = reader.GetString(6),
                Quantity = reader.GetInt32(7),
                UnitPrice = reader.GetDecimal(8),
                DiscountPrice = reader.IsDBNull(9) ? null : reader.GetDecimal(9),
                Subtotal = reader.GetDecimal(10),
            });
        }

        return order;
    }

    public async Task<(List<OrderSummary> Orders, int TotalCount)> GetAllOrdersAsync(string? status, int page, int pageSize)
    {
        const string sql = """
        SELECT COUNT(*) OVER() AS TotalCount,
               o.OrderID,
               o.UserID,
               u.FullName,
               u.Email,
               o.Status,
               o.TotalAmount,
               o.Note,
               o.PaidAt,
               o.CreatedAt,
               (SELECT COUNT(*) FROM dbo.Order_Items oi WHERE oi.OrderID = o.OrderID) AS ItemCount
        FROM dbo.Orders o
        INNER JOIN dbo.Users u ON o.UserID = u.Id
        WHERE (@Status IS NULL OR o.Status = @Status)
        ORDER BY o.CreatedAt DESC
        OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY
        """;

        await using var conn = new SqlConnection(_connectionString);
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@Status", (object?)status ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Offset", (page - 1) * pageSize);
        cmd.Parameters.AddWithValue("@PageSize", pageSize);

        await conn.OpenAsync();
        await using var reader = await cmd.ExecuteReaderAsync();

        var orders = new List<OrderSummary>();
        int totalCount = 0;

        while (await reader.ReadAsync())
        {
            totalCount = reader.GetInt32(0);
            orders.Add(new OrderSummary
            {
                OrderID = reader.GetInt32(1),
                UserID = reader.GetInt32(2),
                FullName = reader.IsDBNull(3) ? null : reader.GetString(3),
                Email = reader.IsDBNull(4) ? null : reader.GetString(4),
                Status = reader.GetString(5),
                TotalAmount = reader.GetDecimal(6),
                Note = reader.IsDBNull(7) ? null : reader.GetString(7),
                PaidAt = reader.IsDBNull(8) ? null : reader.GetDateTime(8),
                CreatedAt = reader.GetDateTime(9),
                ItemCount = reader.GetInt32(10),
            });
        }

        return (orders, totalCount);
    }
}