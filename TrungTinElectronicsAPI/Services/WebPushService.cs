using WebPush;
using System.Text.Json;
using TrungTinElectronicsAPI.Repositories;

namespace TrungTinElectronicsAPI.Services;

public class WebPushService
{
    private readonly PushSubscriptionRepository _subscriptionRepo;
    private readonly WebPushClient _pushClient;

    public WebPushService(PushSubscriptionRepository subscriptionRepo, IConfiguration config)
    {
        _subscriptionRepo = subscriptionRepo;
        _pushClient = new WebPushClient();

        var vapidDetails = new VapidDetails(
            subject: "mailto:admin@trungtinelectronics.com",
            publicKey: config["Vapid:PublicKey"]!,
            privateKey: config["Vapid:PrivateKey"]!
        );
        _pushClient.SetVapidDetails(vapidDetails);
    }

    public async Task SendToUserAsync(int userId, string title, string message, string? url = null)
    {
        var subscriptions = await _subscriptionRepo.GetByUserIdAsync(userId);

        var payload = JsonSerializer.Serialize(new
        {
            title,
            body = message,
            url = url ?? "/",
            icon = "/assets/images/logo.png"
        });

        foreach (var sub in subscriptions)
        {
            try
            {
                var pushSubscription = new WebPush.PushSubscription(sub.Endpoint, sub.P256dh, sub.Auth);
                await _pushClient.SendNotificationAsync(pushSubscription, payload);
            }
            catch (WebPushException)
            {
                // Subscription expired/invalid → xóa
                await _subscriptionRepo.DeleteAsync(sub.UserId, sub.Endpoint);
            }
        }
    }
}
