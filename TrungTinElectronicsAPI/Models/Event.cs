using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TrungTinElectronicsAPI.Models
{
    public class Event
    {
        [Key]
        public int EventId { get; set; }
        public required string Name { get; set; }
        public string? Description { get; set; }
        [Column(TypeName = "decimal(5,2)")]
        public decimal DiscountPercent { get; set; }
        public string? ColorTheme { get; set; }
        public string? BannerUrl { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedDate { get; set; } = DateTime.UtcNow;
        public int TotalProducts { get; set; }
    }
}
