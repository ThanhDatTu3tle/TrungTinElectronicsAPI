using System.ComponentModel.DataAnnotations;

namespace TrungTinElectronicsAPI.Models
{
    public class User
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(100)]
        public string Username { get; set; } = string.Empty;

        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        [MaxLength(255)]
        public string? Email { get; set; }

        public DateTime? DateOfBirth { get; set; }

        [MaxLength(255)]
        public string? FullName { get; set; }

        [MaxLength(10)]
        [MinLength(10)]
        [RegularExpression(@"^\d{10}$", ErrorMessage = "Số điện thoại phải chính xác 10 chữ số")]
        public string? Phone { get; set; }

        [MaxLength(50)]
        public string Role { get; set; } = "user"; // mặc định là User

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}
