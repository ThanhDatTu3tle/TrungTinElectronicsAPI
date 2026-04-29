using TrungTinElectronicsAPI.Models;
using TrungTinElectronicsAPI.Repositories;

namespace TrungTinElectronicsAPI.Services
{
    public class NotificationService : INotificationService
    {
        private readonly INotificationRepository _notificationRepository;

        public NotificationService(INotificationRepository notificationRepository)
        {
            _notificationRepository = notificationRepository;
        }

        public async Task<(IEnumerable<Notification> Items, int TotalCount)> GetAllAsync(int page, int pageSize)
        {
            return await _notificationRepository.GetAllAsync(page, pageSize);
        }

        public async Task<int> GetUnreadCountAsync()
        {
            return await _notificationRepository.GetUnreadCountAsync();
        }

        public async Task<bool> MarkAsReadAsync(int notificationId)
        {
            return await _notificationRepository.MarkAsReadAsync(notificationId);
        }

        public async Task<bool> MarkAllAsReadAsync()
        {
            return await _notificationRepository.MarkAllAsReadAsync();
        }

        public async Task<bool> DeleteAsync(int notificationId)
        {
            return await _notificationRepository.DeleteAsync(notificationId);
        }

        public async Task<Notification> CreateOrderNotificationAsync(int orderId, string customerName, decimal totalAmount)
        {
            var notification = new Notification
            {
                Title = $"Đơn hàng mới #{orderId}",
                Message = $"Khách hàng {customerName} vừa đặt đơn #{orderId} trị giá {totalAmount:N0}đ",
                Type = "new_order",
                OrderId = orderId,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };

            return await _notificationRepository.CreateAsync(notification);
        }

        public async Task<Notification> CreatePaymentNotificationAsync(int orderId, decimal amount)
        {
            var notification = new Notification
            {
                Title = $"Thanh toán thành công #{orderId}",
                Message = $"Đơn hàng #{orderId} đã được thanh toán {amount:N0}đ",
                Type = "payment_confirmed",
                OrderId = orderId,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };

            return await _notificationRepository.CreateAsync(notification);
        }

        public async Task<Notification> CreateCancelNotificationAsync(int orderId)
        {
            var notification = new Notification
            {
                Title = $"Đơn hàng bị huỷ #{orderId}",
                Message = $"Đơn hàng #{orderId} đã bị huỷ do hết thời gian thanh toán",
                Type = "order_cancelled",
                OrderId = orderId,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };

            return await _notificationRepository.CreateAsync(notification);
        }

        public async Task<int> CleanupOldNotificationsAsync(int retentionDays = 90)
        {
            return await _notificationRepository.CleanupOldNotificationsAsync(retentionDays);
        }
    }
}
