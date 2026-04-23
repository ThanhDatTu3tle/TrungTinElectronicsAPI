using Dapper;
using Microsoft.Data.SqlClient;
using TrungTinElectronicsAPI.Data;

namespace TrungTinElectronicsAPI.Repositories.BulkImport;

public class ProductBulkImportRepository : IProductBulkImportRepository
{
    private readonly AppDbContext _context;
    private readonly IConfiguration _config;

    public ProductBulkImportRepository(AppDbContext context, IConfiguration config)
    {
        _context = context;
        _config = config;
    }

    public async Task<Dictionary<string, int>> GetCategoryNameToIdMapAsync()
    {
        using var conn = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
        var rows = await conn.QueryAsync<(string Name, int Id)>(
            "SELECT CategoryName, CategoryId FROM Category WHERE Status = 1"
        );
        return rows.ToDictionary(
            r => r.Name.Trim(),
            r => r.Id,
            StringComparer.OrdinalIgnoreCase
        );
    }

    public async Task BulkInsertProductsAsync(
        IEnumerable<TrungTinElectronicsAPI.Models.Product> products)
    {
        _context.Products.AddRange(products);
        await _context.SaveChangesAsync();
    }
}
