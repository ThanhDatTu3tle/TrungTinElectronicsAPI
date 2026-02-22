using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TrungTinElectronicsAPI.Models
{
    public class Product
    {
        [Key]
        public int ProductId { get; set; }

        [Required]
        public int CategoryId { get; set; }

        [Required]
        [MaxLength(100)]
        public required string ProductName { get; set; }

        public string? Description { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }

        public string? ImageUrl { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedDate { get; set; } = DateTime.UtcNow;

        [MaxLength(50)]
        public string? Code { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? DiscountPrice { get; set; }

        [Required]
        [MaxLength(10)]
        public required string Currency { get; set; }

        [Required]
        [MaxLength(100)]
        public required string Brand { get; set; }

        public int Stock { get; set; }
        public bool IsNew { get; set; }
        public bool IsFeatured { get; set; }

        public string? categoryName { get; set; }
        // Navigation property
        //public virtual required Category Category { get; set; }

        public virtual ICollection<ProductTag> ProductTags { get; set; } = new List<ProductTag>();
    }
}
