using Dapper;
using Microsoft.Data.SqlClient;
using TrungTinElectronicsAPI.Models;

namespace TrungTinElectronicsAPI.Repositories;

public class PushSubscriptionRepository
{
    private readonly string _connectionString;

    public PushSubscriptionRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")!;
    }

    public async Task SaveAsync(int userId, PushSubscriptionRequest request)
    {
        using var connection = new SqlConnection(_connectionString);
        var sql = @"
            MERGE PushSubscriptions AS target
            USING (SELECT @Endpoint AS Endpoint) AS source
            ON target.Endpoint = source.Endpoint
            WHEN MATCHED THEN
                UPDATE SET UserId = @UserId, P256dh = @P256dh, Auth = @Auth
            WHEN NOT MATCHED THEN
                INSERT (UserId, Endpoint, P256dh, Auth)
                VALUES (@UserId, @Endpoint, @P256dh, @Auth);";

        await connection.ExecuteAsync(sql, new
        {
            UserId = userId,
            request.Endpoint,
            request.P256dh,
            request.Auth
        });
    }

    public async Task DeleteAsync(int userId, string endpoint)
    {
        using var connection = new SqlConnection(_connectionString);
        var sql = "DELETE FROM PushSubscriptions WHERE UserId = @UserId AND Endpoint = @Endpoint";
        await connection.ExecuteAsync(sql, new { UserId = userId, Endpoint = endpoint });
    }

    public async Task<IEnumerable<PushSubscription>> GetByUserIdAsync(int userId)
    {
        using var connection = new SqlConnection(_connectionString);
        var sql = "SELECT * FROM PushSubscriptions WHERE UserId = @UserId";
        return await connection.QueryAsync<PushSubscription>(sql, new { UserId = userId });
    }
}
