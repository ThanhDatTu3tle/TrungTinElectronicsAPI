namespace TrungTinElectronicsAPI.Models
{
    public class CreateProductRequest
    {
        public string CategoryId { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public string? ImageUrl { get; set; }
        public string? Code { get; set; }
        public decimal? DiscountPrice { get; set; }
        public string Currency { get; set; } = "VND";
        public string? Brand { get; set; }
        public int Stock { get; set; } = 0;
        public bool IsNew { get; set; } = false;
        public bool IsFeatured { get; set; } = false;
        public bool IsSpotlight { get; set; } = false;
    }
}
