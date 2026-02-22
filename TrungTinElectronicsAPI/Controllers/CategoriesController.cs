using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TrungTinElectronicsAPI.Models.DTOs;
using TrungTinElectronicsAPI.Repositories.Category;
using TrungTinElectronicsAPI.Repositories.Product;

namespace TrungTinElectronicsAPI.Controllers
{
    [Authorize(Roles = "admin")]
    [ApiController]
    [Route("api/[controller]")]
    public class CategoriesController : ControllerBase
    {
        private readonly ICategoryRepository _categoryRepository;

        public CategoriesController(ICategoryRepository categoryRepository)
        {
            _categoryRepository = categoryRepository;
        }

        // POST: api/Categories/GetAllCategories
        [AllowAnonymous]
        [HttpPost("GetAllCategories")]
        public async Task<IActionResult> GetAllCategories(
        [FromQuery] string? categoryName)
        {
            try
            {
                var categories = await _categoryRepository.GetAllCategoriesAsync(categoryName);

                return Ok(new
                {
                    message = "success",
                    result = StatusCodes.Status200OK,
                    data = categories
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

        // POST: api/Categories/CreateCategory
        [HttpPost("CreateCategory")]
        public async Task<IActionResult> CreateCategory(
            [FromQuery] string? categoryName,
            [FromQuery] string? description,
            [FromQuery] string? key,
            [FromQuery] string? icon,
            [FromQuery] int status = 1)
        {
            try
            {
                var newCategoryId = await _categoryRepository.CreateCategoryAsync(
                    categoryName, description, key, icon, status);

                return Ok(new
                {
                    message = "Category created successfully!",
                    result = StatusCodes.Status200OK,
                    productId = newCategoryId
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

        // POST: api/Categories/UpdateCategory
        [HttpPost("UpdateCategory")]
        public async Task<IActionResult> UpdateCategory(
            [FromQuery] int categoryId,
            [FromQuery] string? categoryName,
            [FromQuery] string? description,
            [FromQuery] string? key,
            [FromQuery] string? icon,
            [FromQuery] string action)
        {
            try
            {
                var newCategoryId = await _categoryRepository.UpdateCategoryAsync(
                    categoryId, categoryName, description, key, icon, action);

                if (action == "update")
                {
                    return Ok(new
                    {
                        message = "Update category successfully!",
                        result = StatusCodes.Status200OK,
                        productId = newCategoryId
                    });
                } else
                {
                    return Ok(new
                    {
                        message = "Deleted category successfully!",
                        result = StatusCodes.Status200OK,
                        productId = newCategoryId
                    });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new ApiResponse<object>(
                    message: "Đã xảy ra lỗi: " + ex.Message,
                    result: StatusCodes.Status500InternalServerError
                ));
            }
        }
    }
}
