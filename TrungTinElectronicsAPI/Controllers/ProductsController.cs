using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TrungTinElectronicsAPI.Models;
using TrungTinElectronicsAPI.Models.DTOs;
using TrungTinElectronicsAPI.Services;

namespace TrungTinElectronicsAPI.Controllers
{
    [Authorize(Roles = "admin")]
    [ApiController]
    [Route("api/[controller]")]
    public class ProductsController : ControllerBase
    {
        private readonly IProductRepository _productRepository;
        private readonly CloudinaryService _cloudinaryService;
        private readonly IProductBulkImportService _bulkImportService;

        public ProductsController(
            IProductRepository productRepository,
            CloudinaryService cloudinaryService)
        {
            _productRepository = productRepository;
            _cloudinaryService = cloudinaryService;
        }

        // POST: api/Products/GetAllProducts
        [AllowAnonymous]
        [HttpPost("GetAllProducts")]
        public async Task<IActionResult> GetAllProducts(
        [FromQuery] string? code,
        [FromQuery] string? categoryId,
        [FromQuery] string? brand,
        [FromQuery] decimal? minPrice,
        [FromQuery] decimal? maxPrice,
        [FromQuery] bool? isNew,
        [FromQuery] bool? isFeatured,
        [FromQuery] bool? isSpotlight)
        {
            try 
            {
                var products = await _productRepository.GetAllProductsAsync(
                    code, categoryId, brand, minPrice, maxPrice, isNew, isFeatured, isSpotlight);

                return Ok(new
                {
                    message = "success",
                    result = StatusCodes.Status200OK,
                    data = products
                });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new ApiResponse<object>(
                    message: "Đã xảy ra lỗi: " + ex.Message,
                    result: StatusCodes.Status500InternalServerError
                ));
            }
        }

        // POST: api/Products/GetProductByCode
        [AllowAnonymous]
        [HttpPost("GetProductByCode")]
        public async Task<IActionResult> GetProductByCode([FromQuery] string? code)
        {
            try
            {
                var product = await _productRepository.GetProductByCodeAsync(code);

                if (product == null)
                {
                    return NotFound(new ApiResponse<object>(
                        message: "Không tìm thấy sản phẩm với Code này.",
                        result: StatusCodes.Status404NotFound
                    ));
                }

                return Ok(new
                {
                    message = "success",
                    result = StatusCodes.Status200OK,
                    data = product
                });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new ApiResponse<object>(
                    message: "Đã xảy ra lỗi: " + ex.Message,
                    result: StatusCodes.Status500InternalServerError
                ));
            }
        }

        // POST: api/Products/CreateProduct
        [HttpPost("CreateProduct")]
        public async Task<IActionResult> CreateProduct([FromBody] CreateProductRequest req)
        {
            try
            {
                var newProductId = await _productRepository.CreateProductAsync(
                    req.CategoryId, req.ProductName, req.Description, req.Price, req.ImageUrl,
                    req.Code, req.DiscountPrice, req.Currency, req.Brand,
                    req.Stock, req.IsNew, req.IsFeatured, req.IsSpotlight);

                return Ok(new
                {
                    message = "Product created successfully!",
                    result = StatusCodes.Status200OK,
                    productId = newProductId
                });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new ApiResponse<object>(
                    message: "Đã xảy ra lỗi: " + ex.Message,
                    result: StatusCodes.Status500InternalServerError
                ));
            }
        }

        // POST: api/Products/EditProduct
        //[HttpPost("EditProduct")]
        //public async Task<IActionResult> EditProduct([FromBody] Product product)
        //{
        //    var updated = await _productRepository.UpdateProductAsync(product);
        //    return Ok(updated);
        //}

        // POST: api/Products/DeleteProduct
        //[HttpPost("DeleteProduct")]
        //public async Task<IActionResult> DeleteProduct([FromBody] string productId)
        //{
        //    var deleted = await _productRepository.DeleteProductAsync(productId);
        //    return Ok(deleted);
        //}

        // POST: api/Products/UploadImage
        [HttpPost("UploadImage")]
        public async Task<IActionResult> UploadImage(IFormFile file)
        {
            try
            {
                var imageUrl = await _cloudinaryService.UploadImageAsync(file);
                return Ok(new { url = imageUrl });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = "Upload failed.", message = ex.Message });
            }
        }

        [HttpPost("BulkImport")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> BulkImport(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { message = "Vui lòng chọn file .xlsx" });

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (ext != ".xlsx")
                return BadRequest(new { message = "Chỉ hỗ trợ file .xlsx" });

            const long maxSize = 10 * 1024 * 1024; // 10MB
            if (file.Length > maxSize)
                return BadRequest(new { message = "File không được vượt quá 10MB" });

            var result = await _bulkImportService.ImportAsync(file);

            return Ok(new
            {
                message = "success",
                result.SuccessCount,
                result.Errors
            });
        }
    }
}
