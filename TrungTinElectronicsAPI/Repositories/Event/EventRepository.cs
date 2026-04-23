using Dapper;
using Microsoft.Data.SqlClient;
using System.Data;
using TrungTinElectronicsAPI.Models;

namespace TrungTinElectronicsAPI.Repositories.Event
{
    public class EventRepository : IEventRepository
    {
        private readonly string _connectionString;

        public EventRepository(IConfiguration config)
        {
            _connectionString = config.GetConnectionString("DefaultConnection");
        }

        public async Task<IEnumerable<TrungTinElectronicsAPI.Models.Event>> GetAllEventsAsync(bool? isActive = null)
        {
            using var connection = new SqlConnection(_connectionString);
            return await connection.QueryAsync<TrungTinElectronicsAPI.Models.Event>(
                "[dbo].[GetAllEvents]",
                new { IsActive = isActive },
                commandType: CommandType.StoredProcedure
            );
        }

        public async Task<IEnumerable<TrungTinElectronicsAPI.Models.Event>> GetActiveEventsAsync()
        {
            using var connection = new SqlConnection(_connectionString);
            return await connection.QueryAsync<TrungTinElectronicsAPI.Models.Event>(
                "[dbo].[GetActiveEvents]",
                commandType: CommandType.StoredProcedure
            );
        }

        public async Task<int> CreateEventAsync(
            string name,
            string? description,
            decimal discountPercent,
            string? colorTheme,
            string? bannerUrl,
            DateTime startDate,
            DateTime endDate,
            bool isActive = true)
        {
            using var connection = new SqlConnection(_connectionString);
            var parameters = new DynamicParameters();
            parameters.Add("@Name", name);
            parameters.Add("@Description", description);
            parameters.Add("@DiscountPercent", discountPercent);
            parameters.Add("@ColorTheme", colorTheme);
            parameters.Add("@BannerUrl", bannerUrl);
            parameters.Add("@StartDate", startDate);
            parameters.Add("@EndDate", endDate);
            parameters.Add("@IsActive", isActive);
            parameters.Add("@NewEventId", dbType: DbType.Int32, direction: ParameterDirection.Output);

            await connection.ExecuteAsync(
                "[dbo].[CreateEvent]",
                parameters,
                commandType: CommandType.StoredProcedure
            );

            return parameters.Get<int>("@NewEventId");
        }

        public async Task<string> UpdateEventAsync(
            int eventId,
            string? name,
            string? description,
            decimal? discountPercent,
            string? colorTheme,
            string? bannerUrl,
            DateTime? startDate,
            DateTime? endDate,
            bool? isActive,
            string action = "update")
        {
            using var connection = new SqlConnection(_connectionString);
            var parameters = new DynamicParameters();
            parameters.Add("@EventId", eventId);
            parameters.Add("@Name", name);
            parameters.Add("@Description", description);
            parameters.Add("@DiscountPercent", discountPercent);
            parameters.Add("@ColorTheme", colorTheme);
            parameters.Add("@BannerUrl", bannerUrl);
            parameters.Add("@StartDate", startDate);
            parameters.Add("@EndDate", endDate);
            parameters.Add("@IsActive", isActive);
            parameters.Add("@Action", action);
            parameters.Add("@ResultMessage", dbType: DbType.String, size: 500, direction: ParameterDirection.Output);

            await connection.ExecuteAsync(
                "[dbo].[UpdateEvent]",
                parameters,
                commandType: CommandType.StoredProcedure
            );

            return parameters.Get<string>("@ResultMessage");
        }

        public async Task<string> AssignProductToEventAsync(int productId, int? eventId)
        {
            using var connection = new SqlConnection(_connectionString);
            var parameters = new DynamicParameters();
            parameters.Add("@ProductId", productId);
            parameters.Add("@EventId", eventId);
            parameters.Add("@ResultMessage", dbType: DbType.String, size: 500, direction: ParameterDirection.Output);

            await connection.ExecuteAsync(
                "[dbo].[AssignProductToEvent]",
                parameters,
                commandType: CommandType.StoredProcedure
            );

            return parameters.Get<string>("@ResultMessage");
        }
    }
}
