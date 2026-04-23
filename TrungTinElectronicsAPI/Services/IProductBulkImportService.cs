using TrungTinElectronicsAPI.Models;

namespace TrungTinElectronicsAPI.Services;

public interface IProductBulkImportService
{
    Task<BulkImportResult> ImportAsync(IFormFile file);
}
