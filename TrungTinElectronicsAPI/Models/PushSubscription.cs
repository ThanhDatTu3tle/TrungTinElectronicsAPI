namespace TrungTinElectronicsAPI.Models;

public class PushSubscription
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Endpoint { get; set; } = string.Empty;
    public string P256dh { get; set; } = string.Empty;
    public string Auth { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class PushSubscriptionRequest
{
    public string Endpoint { get; set; } = string.Empty;
    public string P256dh { get; set; } = string.Empty;
    public string Auth { get; set; } = string.Empty;
}
