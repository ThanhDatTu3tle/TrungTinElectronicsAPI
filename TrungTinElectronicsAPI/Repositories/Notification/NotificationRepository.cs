using Dapper;
using Microsoft.Data.SqlClient;
using TrungTinElectronicsAPI.Models;

namespace TrungTinElectronicsAPI.Repositories
{
    public class NotificationRepository : INotificationRepository
    {
        private readonly string _connectionString;

        public NotificationRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")!;
        }

        public async Task<(IEnumerable<Notification> Items, int TotalCount)> GetAllAsync(int page, int pageSize)
        {
            using var connection = new SqlConnection(_connectionString);

            var sql = @"
                SELECT COUNT(*) FROM Notifications;
 
                SELECT NotificationId, Title, Message, Type, OrderId, IsRead, CreatedAt
                FROM Notifications
                ORDER BY CreatedAt DESC
                OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;";

            using var multi = await connection.QueryMultipleAsync(sql, new
            {
                Offset = (page - 1) * pageSize,
                PageSize = pageSize
            });

            var totalCount = await multi.ReadSingleAsync<int>();
            var items = await multi.ReadAsync<Notification>();

            return (items, totalCount);
        }

        public async Task<int> GetUnreadCountAsync()
        {
            using var connection = new SqlConnection(_connectionString);
            var sql = "SELECT COUNT(*) FROM Notifications WHERE IsRead = 0";

            return await connection.ExecuteScalarAsync<int>(sql);
        }

        public async Task<bool> MarkAsReadAsync(int notificationId)
        {
            using var connection = new SqlConnection(_connectionString);
            var sql = "UPDATE Notifications SET IsRead = 1 WHERE NotificationId = @NotificationId";

            var affected = await connection.ExecuteAsync(sql, new { NotificationId = notificationId });
            return affected > 0;
        }

        public async Task<bool> MarkAllAsReadAsync()
        {
            using var connection = new SqlConnection(_connectionString);
            var sql = "UPDATE Notifications SET IsRead = 1 WHERE IsRead = 0";

            await connection.ExecuteAsync(sql);
            return true;
        }

        public async Task<bool> DeleteAsync(int notificationId)
        {
            using var connection = new SqlConnection(_connectionString);
            var sql = "DELETE FROM Notifications WHERE NotificationId = @NotificationId";

            var affected = await connection.ExecuteAsync(sql, new { NotificationId = notificationId });
            return affected > 0;
        }

        public async Task<Notification> CreateAsync(Notification notification)
        {
            using var connection = new SqlConnection(_connectionString);
            var sql = @"
                INSERT INTO Notifications (Title, Message, Type, OrderId, IsRead, CreatedAt)
                OUTPUT INSERTED.NotificationId, INSERTED.Title, INSERTED.Message,
                       INSERTED.Type, INSERTED.OrderId, INSERTED.IsRead, INSERTED.CreatedAt
                VALUES (@Title, @Message, @Type, @OrderId, @IsRead, @CreatedAt)";

            return await connection.QuerySingleAsync<Notification>(sql, notification);
        }

        public async Task<int> CleanupOldNotificationsAsync(int retentionDays)
        {
            using var connection = new SqlConnection(_connectionString);
            var sql = @"
                DELETE FROM Notifications
                WHERE IsRead = 1 AND CreatedAt < DATEADD(DAY, -@RetentionDays, GETUTCDATE())";

            return await connection.ExecuteAsync(sql, new { RetentionDays = retentionDays });
        }
    }
}
