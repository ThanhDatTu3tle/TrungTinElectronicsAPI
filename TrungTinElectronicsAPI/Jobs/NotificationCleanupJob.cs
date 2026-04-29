using TrungTinElectronicsAPI.Services;

namespace TrungTinElectronicsAPI.Jobs
{
    public class NotificationCleanupJob
    {
        private readonly INotificationService _notificationService;
        private readonly ILogger<NotificationCleanupJob> _logger;

        public NotificationCleanupJob(
            INotificationService notificationService,
            ILogger<NotificationCleanupJob> logger)
        {
            _notificationService = notificationService;
            _logger = logger;
        }

        public async Task Execute()
        {
            // Xoá notification đã đọc và cũ hơn 90 ngày
            var deletedCount = await _notificationService.CleanupOldNotificationsAsync(retentionDays: 90);
            _logger.LogInformation("NotificationCleanupJob: Đã xoá {Count} notification cũ", deletedCount);
        }
    }
}
