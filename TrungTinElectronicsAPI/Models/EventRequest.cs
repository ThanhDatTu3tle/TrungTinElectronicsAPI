namespace TrungTinElectronicsAPI.Models
{
    public class UpdateEventRequest
    {
        public int EventId { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public decimal? DiscountPercent { get; set; }
        public string? ColorTheme { get; set; }
        public string? BannerUrl { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public bool? IsActive { get; set; }
        public string Action { get; set; } = "update";
    }

    public class CreateEventRequest
    {
        public string Name { get; set; } = "";
        public string? Description { get; set; }
        public decimal DiscountPercent { get; set; }
        public string? ColorTheme { get; set; }
        public string? BannerUrl { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class AssignProductRequest
    {
        public int ProductId { get; set; }
        public int? EventId { get; set; }
    }
}
