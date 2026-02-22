using System.ComponentModel.DataAnnotations;

namespace TrungTinElectronicsAPI.Models
{
    public class Category
    {
        [Key]
        public int CategoryId { get; set; }

        [Required]
        [MaxLength(100)]
        public required string CategoryName { get; set; }

        public string? Description { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedDate { get; set; } = DateTime.UtcNow;

        public required string Key { get; set; }
        public required string Icon { get; set; }
        public int Status { get; set; }

        // Navigation property
        public virtual required ICollection<Product> Products { get; set; }
    }
}
