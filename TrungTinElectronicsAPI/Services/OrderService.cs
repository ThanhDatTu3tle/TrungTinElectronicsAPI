using TrungTinElectronics.Models;
using TrungTinElectronics.Repositories;
using TrungTinElectronicsAPI.Services;

namespace TrungTinElectronics.Services;

public class OrderService
{
    private readonly OrderRepository _repo;
    private readonly INotificationService _notificationService;
    private readonly WebPushService _webPushService;

    public OrderService(OrderRepository repo, INotificationService notificationService, WebPushService webPushService)
    {
        _repo = repo;
        _notificationService = notificationService;
        _webPushService = webPushService;
    }

    public async Task<CreateOrderResponse> CreateOrderAsync(CreateOrderRequest request)
    {
        if (!request.Items.Any())
            return Fail("Giỏ hàng trống");

        if (request.Items.Any(i => i.Quantity <= 0))
            return Fail("Số lượng không hợp lệ");

        var (orderId, error) = await _repo.CreateOrderAsync(
            request.UserID, request.Note, request.Items);

        if (!string.IsNullOrEmpty(error) || orderId == 0)
            return Fail(error ?? "Tạo đơn thất bại");

        var detail = await _repo.GetOrderDetailAsync(orderId);

        // Auto tạo notification cho admin
        await _notificationService.CreateOrderNotificationAsync(
            orderId: orderId,
            customerName: "Khách hàng",
            totalAmount: detail?.TotalAmount ?? 0
        );

        return new CreateOrderResponse
        {
            Success = true,
            OrderID = orderId,
            TotalAmount = detail?.TotalAmount ?? 0,
        };
    }

    public async Task<(bool Success, int OrderID, string? ErrorMessage)> UpdateOrderStatusAsync(int orderId, string status)
    {
        var validStatuses = new[] { "pending_payment", "confirmed", "paid", "cancelled", "payment_failed", "delivered" };
        if (!validStatuses.Contains(status))
            return (false, 0, "Trạng thái không hợp lệ");

        var order = await _repo.GetOrderDetailAsync(orderId);
        if (order is null)
            return (false, 0, "Không tìm thấy đơn hàng");

        if (status == "confirmed" && order.Status != "pending_payment")
            return (false, 0, "Chỉ đơn chờ xác nhận mới được confirm");

        if (status == "paid" && order.Status != "confirmed")
            return (false, 0, "Chỉ đơn đã xác nhận mới được thanh toán");

        if (status == "delivered" && order.Status != "paid")
            return (false, 0, "Chỉ đơn đã thanh toán mới giao hàng được");

        await _repo.UpdateOrderStatusAsync(orderId, status);

        // Gửi Web Push cho user
        if (order.UserID is not null and > 0)
        {
            var (title, message, url) = status switch
            {
                "confirmed" => ("Đơn hàng đã xác nhận", $"Đơn hàng #{orderId} đã được shop xác nhận. Vui lòng thanh toán.", $"/invoice/{orderId}"),
                "paid" => ("Thanh toán thành công", $"Đơn hàng #{orderId} đã được xác nhận thanh toán.", $"/payment-result?orderId={orderId}&status=success"),
                "delivered" => ("Đơn hàng đã giao", $"Đơn hàng #{orderId} đã được giao thành công.", $"/invoice/{orderId}"),
                "cancelled" => ("Đơn hàng bị hủy", $"Đơn hàng #{orderId} đã bị hủy.", $"/invoice/{orderId}"),
                _ => ((string?)null, (string?)null, (string?)null)
            };

            if (title is not null)
                await _webPushService.SendToUserAsync(order.UserID.Value, title, message!, url);
        }

        return (true, orderId, null);
    }

    public async Task<(bool Success, string? ErrorMessage)> ClaimPaidAsync(int orderId)
    {
        var order = await _repo.GetOrderDetailAsync(orderId);
        if (order is null)
            return (false, "Không tìm thấy đơn hàng");

        if (order.Status != "confirmed")
            return (false, "Đơn hàng chưa được xác nhận hoặc đã thanh toán");

        // Tạo notification cho admin
        await _notificationService.CreateClaimPaidNotificationAsync(orderId);

        return (true, null);
    }

    private static CreateOrderResponse Fail(string msg) =>
        new() { Success = false, ErrorMessage = msg };

    public async Task<(List<OrderSummary> Orders, int TotalCount)> GetAllOrdersAsync(
    string? status, int page, int pageSize)
    {
        return await _repo.GetAllOrdersAsync(status, page, pageSize);
    }
}