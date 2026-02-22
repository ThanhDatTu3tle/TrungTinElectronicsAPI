using System.Threading.Tasks;
using TrungTinElectronicsAPI.Models;

public interface IProductRepository
{
    // Lấy toàn bộ danh sách sản phẩm
    Task<IEnumerable<Product>> GetAllProductsAsync(
        string? code = null,
        string? categoryId = null,
        string? brand = null,
        decimal? minPrice = null,
        decimal? maxPrice = null,
        bool? isNew = null,
        bool? isFeatured = null
    );

    // Lấy ra thông tin chi tiết từng sản phẩm
    Task<IEnumerable<Product>> GetProductByCodeAsync(
        string? code = null
    );

    // Tạo sản phẩm mới
    Task<IEnumerable<Product>> CreateProductAsync(
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
        bool isFeatured = false
    );
}
