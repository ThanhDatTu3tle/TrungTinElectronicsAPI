using Microsoft.AspNetCore.Http;

namespace TrungTinElectronicsAPI.Models
{
    public class UploadImageRequest
    {
        public IFormFile File { get; set; } = null!;
    }
}
