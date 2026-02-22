using System.ComponentModel.DataAnnotations;

namespace TrungTinElectronicsAPI.Models
{
    public class ProductTag
    {
        [Key]
        public int ProductTagId { get; set; }

        public int ProductId { get; set; }
        public required string Tag { get; set; }

        // Navigation property
        public required Product Product { get; set; }
    }
}
