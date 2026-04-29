using TrungTinElectronicsAPI.Models;

namespace TrungTinElectronicsAPI.Services
{
    public interface INotificationService
    {
        Task<(IEnumerable<Notification> Items, int TotalCount)> GetAllAsync(int page, int pageSize);
        Task<int> GetUnreadCountAsync();
        Task<bool> MarkAsReadAsync(int notificationId);
        Task<bool> MarkAllAsReadAsync();
        Task<bool> DeleteAsync(int notificationId);
        Task<Notification> CreateOrderNotificationAsync(int orderId, string customerName, decimal totalAmount);
        Task<Notification> CreatePaymentNotificationAsync(int orderId, decimal amount);
        Task<Notification> CreateCancelNotificationAsync(int orderId);
        Task<int> CleanupOldNotificationsAsync(int retentionDays = 90);
    }
}
