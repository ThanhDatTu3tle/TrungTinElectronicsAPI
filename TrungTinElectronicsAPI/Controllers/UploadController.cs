using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TrungTinElectronicsAPI.Models;
using TrungTinElectronicsAPI.Services;

namespace TrungTinElectronicsAPI.Controllers
{
    [Authorize(Roles = "admin")]
    [ApiController]
    [Route("api/uploads")]
    public class UploadController : ControllerBase
    {
        private readonly CloudinaryService _cloudinaryService;

        public UploadController(CloudinaryService cloudinaryService)
        {
            _cloudinaryService = cloudinaryService;
        }

        [HttpPost("image")]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(2 * 1024 * 1024)]
        public async Task<IActionResult> UploadImage([FromForm] UploadImageRequest request)
        {
            if (request.File == null)
                return BadRequest("File is required");

            var imageUrl = await _cloudinaryService.UploadImageAsync(request.File);

            return Ok(new { url = imageUrl });
        }
    }
}
