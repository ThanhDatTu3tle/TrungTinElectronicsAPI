using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace TrungTinElectronicsAPI.Repositories.Category;

public class CategoryRepository : ICategoryRepository
{
    private readonly IConfiguration _config;
    private readonly string _connectionString;

    public CategoryRepository(IConfiguration config)
    {
        _config = config;
        _connectionString = _config.GetConnectionString("DefaultConnection");
    }

    public async Task<IEnumerable<TrungTinElectronicsAPI.Models.Category>> GetAllCategoriesAsync(
        string? categoryName = null)
    {
        using var connection = new SqlConnection(_connectionString);

        var categories = await connection.QueryAsync<TrungTinElectronicsAPI.Models.Category>(
            "[dbo].[GetAllCategories]",
            new
            {
                CategoryName = categoryName
            },
            commandType: CommandType.StoredProcedure
        );

        return categories;
    }

    public async Task<IEnumerable<TrungTinElectronicsAPI.Models.Category>> CreateCategoryAsync(
        string? categoryName = null,
        string? description = null,
        string? key = null,
        string? icon = null,
        int status = 1)
    {
        using var connection = new SqlConnection(_connectionString);
        var parameters = new DynamicParameters();

        parameters.Add("@CategoryName", categoryName);
        parameters.Add("@Description", description);
        parameters.Add("@Key", key);
        parameters.Add("@Icon", icon);
        parameters.Add("@Status", status);

        // output param
        parameters.Add("@NewCategoryId", dbType: DbType.Int32, direction: ParameterDirection.Output);

        await connection.ExecuteAsync(
            "[dbo].[CreateCategory]",
            parameters,
            commandType: CommandType.StoredProcedure
        );

        // Get the output value
        var newCategoryId = parameters.Get<int>("@NewCategoryId");

        // Query the created category by its ID (assuming you have a method for this)
        var createdCategory = await connection.QueryFirstOrDefaultAsync<TrungTinElectronicsAPI.Models.Category>(
            "SELECT * FROM Category WHERE Id = @Id",
            new { Id = newCategoryId }
        );

        // Return as IEnumerable to match interface
        return createdCategory != null ? new[] { createdCategory } : Enumerable.Empty<TrungTinElectronicsAPI.Models.Category>();
    }

    public async Task<IEnumerable<TrungTinElectronicsAPI.Models.Category>> UpdateCategoryAsync(
        int categoryId,
        string? categoryName = null,
        string? description = null,
        string? key = null,
        string? icon = null,
        string? action = null)
    {
        using var connection = new SqlConnection(_connectionString);
        var parameters = new DynamicParameters();

        parameters.Add("@CategoryId", categoryId);
        parameters.Add("@CategoryName", categoryName);
        parameters.Add("@Description", description);
        parameters.Add("@Key", key);
        parameters.Add("@Icon", icon);
        parameters.Add("@Action", action);

        // output param
        parameters.Add("@ResultMessage", dbType: DbType.String, size: 500, direction: ParameterDirection.Output);

        await connection.ExecuteAsync(
            "[dbo].[UpdateCategory]",
            parameters,
            commandType: CommandType.StoredProcedure
        );

        // Get the output value
        var resultMessage = parameters.Get<string>("@ResultMessage");

        // Nếu DELETE → trả về empty
        if (action == "delete")
        {
            return Enumerable.Empty<TrungTinElectronicsAPI.Models.Category>();
        }

        // Nếu UPDATE → SELECT lại category
        var updatedCategory = await connection.QueryFirstOrDefaultAsync<TrungTinElectronicsAPI.Models.Category>(
            "SELECT * FROM Category WHERE CategoryId = @Id",
            new { Id = categoryId }
        );

        // Return as IEnumerable to match interface
        return updatedCategory != null ? new[] { updatedCategory } : Enumerable.Empty<TrungTinElectronicsAPI.Models.Category>();
    }
}
