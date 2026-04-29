namespace TrungTinElectronicsAPI.Models
{
    public class Notification
    {
        public int NotificationId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Type { get; set; } = "new_order"; // new_order, payment_confirmed, order_cancelled
        public int? OrderId { get; set; }
        public bool IsRead { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
