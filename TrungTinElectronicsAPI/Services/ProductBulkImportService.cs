using ClosedXML.Excel;
using TrungTinElectronicsAPI.Models;
using TrungTinElectronicsAPI.Repositories.BulkImport;

namespace TrungTinElectronicsAPI.Services;

public class ProductBulkImportService : IProductBulkImportService
{
    private readonly IProductBulkImportRepository _repo;

    public ProductBulkImportService(IProductBulkImportRepository repo)
    {
        _repo = repo;
    }

    public async Task<BulkImportResult> ImportAsync(IFormFile file)
    {
        var result = new BulkImportResult();
        var validProducts = new List<TrungTinElectronicsAPI.Models.Product>();

        var categoryMap = await _repo.GetCategoryNameToIdMapAsync();

        using var stream = file.OpenReadStream();
        using var workbook = new XLWorkbook(stream);

        var sheet = workbook.Worksheets.FirstOrDefault(ws =>
            ws.Name.Equals("Import", StringComparison.OrdinalIgnoreCase))
            ?? workbook.Worksheet(1);

        var lastRow = sheet.LastRowUsed()?.RowNumber() ?? 1;

        for (int rowNum = 2; rowNum <= lastRow; rowNum++)
        {
            var row = sheet.Row(rowNum);
            if (row.IsEmpty()) continue;

            var code = row.Cell(1).GetString().Trim();
            var productName = row.Cell(2).GetString().Trim();
            var categoryName = row.Cell(3).GetString().Trim();
            var priceStr = row.Cell(4).GetString().Trim();
            var discountStr = row.Cell(5).GetString().Trim();
            var currency = row.Cell(6).GetString().Trim();
            var brand = row.Cell(7).GetString().Trim();
            var stockStr = row.Cell(8).GetString().Trim();
            var description = row.Cell(9).GetString().Trim();
            var isNewStr = row.Cell(10).GetString().Trim();
            var isFeaturedStr = row.Cell(11).GetString().Trim();
            var isSpotlightStr = row.Cell(12).GetString().Trim();

            if (string.IsNullOrEmpty(code))
            {
                result.Errors.Add(new() { Row = rowNum, Code = code, Reason = "Code không được để trống" });
                continue;
            }
            if (string.IsNullOrEmpty(productName))
            {
                result.Errors.Add(new() { Row = rowNum, Code = code, Reason = "ProductName không được để trống" });
                continue;
            }
            if (string.IsNullOrEmpty(categoryName))
            {
                result.Errors.Add(new() { Row = rowNum, Code = code, Reason = "CategoryName không được để trống" });
                continue;
            }
            if (!categoryMap.TryGetValue(categoryName, out int categoryId))
            {
                result.Errors.Add(new() { Row = rowNum, Code = code, Reason = $"CategoryName '{categoryName}' không tồn tại" });
                continue;
            }
            if (!decimal.TryParse(priceStr, out decimal price) || price <= 0)
            {
                result.Errors.Add(new() { Row = rowNum, Code = code, Reason = "Price không hợp lệ hoặc phải > 0" });
                continue;
            }

            decimal? discountPrice = decimal.TryParse(discountStr, out var dp) ? dp : null;
            int stock = int.TryParse(stockStr, out var s) ? s : 0;

            validProducts.Add(new TrungTinElectronicsAPI.Models.Product
            {
                Code = code,
                ProductName = productName,
                CategoryId = categoryId,
                Price = price,
                DiscountPrice = discountPrice,
                Currency = string.IsNullOrEmpty(currency) ? "VND" : currency,
                Brand = brand,
                Stock = stock,
                Description = description,
                IsNew = ParseBool(isNewStr),
                IsFeatured = ParseBool(isFeaturedStr),
                IsSpotlight = ParseBool(isSpotlightStr),
                ImageUrl = null,
            });
        }

        if (validProducts.Any())
            await _repo.BulkInsertProductsAsync(validProducts);

        result.SuccessCount = validProducts.Count;
        return result;
    }

    private static bool ParseBool(string value) =>
        value == "1" || value.Equals("true", StringComparison.OrdinalIgnoreCase);
}
