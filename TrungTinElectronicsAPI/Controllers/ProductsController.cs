using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
        [FromQuery] bool? isFeatured)
        {
            try 
            {
                var products = await _productRepository.GetAllProductsAsync(
                    code, categoryId, brand, minPrice, maxPrice, isNew, isFeatured);

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
        public async Task<IActionResult> CreateProduct(
            [FromQuery] string? categoryId,
            [FromQuery] string? productName,
            [FromQuery] string? description,
            [FromQuery] decimal? price,
            [FromQuery] string? imageUrl,
            [FromQuery] string? code,
            [FromQuery] decimal? discountPrice,
            [FromQuery] string? currency,
            [FromQuery] string? brand,
            [FromQuery] int stock = 0,
            [FromQuery] bool isNew = false,
            [FromQuery] bool isFeatured = false)
        {
            try
            {
                var newProductId = await _productRepository.CreateProductAsync(
                    categoryId, productName, description, price, imageUrl,
                    code, discountPrice, currency, brand, stock, isNew, isFeatured);

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
    }
}
