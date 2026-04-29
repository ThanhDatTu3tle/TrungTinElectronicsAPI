using TrungTinElectronicsAPI.Models;

namespace TrungTinElectronicsAPI.Repositories;

public interface INotificationRepository
{
    Task<(IEnumerable<Notification> Items, int TotalCount)> GetAllAsync(int page, int pageSize);
    Task<int> GetUnreadCountAsync();
    Task<bool> MarkAsReadAsync(int notificationId);
    Task<bool> MarkAllAsReadAsync();
    Task<bool> DeleteAsync(int notificationId);
    Task<Notification> CreateAsync(Notification notification);
    Task<int> CleanupOldNotificationsAsync(int retentionDays);
}
