using TrungTinElectronicsAPI.Models;

namespace TrungTinElectronicsAPI.Repositories.BulkImport;

public interface IProductBulkImportRepository
{
    Task<Dictionary<string, int>> GetCategoryNameToIdMapAsync();
    Task BulkInsertProductsAsync(IEnumerable<TrungTinElectronicsAPI.Models.Product> products);
}
