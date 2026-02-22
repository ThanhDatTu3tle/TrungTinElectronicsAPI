using TrungTinElectronicsAPI.Models;

public interface ICategoryRepository
{
    // Lấy toàn bộ danh sách danh mục
    Task<IEnumerable<Category>> GetAllCategoriesAsync(
        string? categoryName = null
    );

    // Tạo danh mục mới
    Task<IEnumerable<Category>> CreateCategoryAsync(
        string? categoryName = null,
        string? description = null,
        string? key = null,
        string? icon = null,
        int status = 1
    );

    // Cập nhật/Xóa danh mục
    Task<IEnumerable<Category>> UpdateCategoryAsync(
        int categoryId,
        string? categoryName = null,
        string? description = null,
        string? key = null,
        string? icon = null,
        string? action = null
    );
}
