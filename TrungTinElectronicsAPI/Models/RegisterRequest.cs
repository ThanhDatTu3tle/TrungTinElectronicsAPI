using System.ComponentModel.DataAnnotations;

namespace TrungTinElectronicsAPI.Models
{
    public class RegisterRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? FullName { get; set; }
        public string? Phone { get; set; }
        public string? Role { get; set; }
    }
}
