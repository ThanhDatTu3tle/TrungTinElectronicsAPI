using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Threading.Tasks;
using TrungTinElectronicsAPI.Models;

namespace TrungTinElectronicsAPI.Repositories.Product;

public class ProductRepository : IProductRepository
{
    private readonly IConfiguration _config;
    private readonly string _connectionString;

    public ProductRepository(IConfiguration config)
    {
        _config = config;
        _connectionString = _config.GetConnectionString("DefaultConnection");
    }

    public async Task<IEnumerable<TrungTinElectronicsAPI.Models.Product>> GetAllProductsAsync(
        string? code = null,
        string? categoryId = null,
        string? brand = null,
        decimal? minPrice = null,
        decimal? maxPrice = null,
        bool? isNew = null,
        bool? isFeatured = null)
    {
        using var connection = new SqlConnection(_connectionString);

        var products = await connection.QueryAsync<TrungTinElectronicsAPI.Models.Product>(
            "[dbo].[GetAllProducts]",
            new
            {
                Code = code,
                CategoryId = categoryId,
                Brand = brand,
                MinPrice = minPrice,
                MaxPrice = maxPrice,
                IsNew = isNew,
                IsFeatured = isFeatured
            },
            commandType: CommandType.StoredProcedure
        );

        return products;
    }

    public async Task<IEnumerable<TrungTinElectronicsAPI.Models.Product>> GetProductByCodeAsync(string? code = null)
    {
        using var connection = new SqlConnection(_connectionString);

        var product = await connection.QueryAsync<TrungTinElectronicsAPI.Models.Product>(
            "[dbo].[GetProductByCode]",
            new
            {
                Code = code
            },
            commandType: CommandType.StoredProcedure
        );

        return product;
    }

    public async Task<IEnumerable<TrungTinElectronicsAPI.Models.Product>> CreateProductAsync(
        string? categoryId = null,
        string? productName = null,
        string? description = null,
        decimal? price = null,
        string? imageUrl = null,
        string? code = null,
        decimal? discountPrice = null,
        string? currency = "VND",
        string? brand = null,
        int stock = 0,
        bool isNew = false,
        bool isFeatured = false)
    {
        using var connection = new SqlConnection(_connectionString);
        var parameters = new DynamicParameters();

        parameters.Add("@CategoryId", categoryId);
        parameters.Add("@ProductName", productName);
        parameters.Add("@Description", description);
        parameters.Add("@Price", price);
        parameters.Add("@ImageUrl", imageUrl);
        parameters.Add("@Code", code);
        parameters.Add("@DiscountPrice", discountPrice);
        parameters.Add("@Currency", currency);
        parameters.Add("@Brand", brand);
        parameters.Add("@Stock", stock);
        parameters.Add("@IsNew", isNew);
        parameters.Add("@IsFeatured", isFeatured);

        // output param
        parameters.Add("@NewProductId", dbType: DbType.Int32, direction: ParameterDirection.Output);

        await connection.ExecuteAsync(
            "[dbo].[CreateProduct]",
            parameters,
            commandType: CommandType.StoredProcedure
        );

        // Get the output value
        var newProductId = parameters.Get<int>("@NewProductId");

        // Query the created product by its ID (assuming you have a method for this)
        var createdProduct = await connection.QueryFirstOrDefaultAsync<TrungTinElectronicsAPI.Models.Product>(
            "SELECT * FROM Product WHERE Id = @Id",
            new { Id = newProductId }
        );

        // Return as IEnumerable to match interface
        return createdProduct != null ? new[] { createdProduct } : Enumerable.Empty<TrungTinElectronicsAPI.Models.Product>();
    }
}
