using TrungTinElectronicsAPI.Models;

public interface IEventRepository
{
    Task<IEnumerable<Event>> GetAllEventsAsync(bool? isActive = null);
    Task<IEnumerable<Event>> GetActiveEventsAsync();
    Task<int> CreateEventAsync(
        string name,
        string? description,
        decimal discountPercent,
        string? colorTheme,
        string? bannerUrl,
        DateTime startDate,
        DateTime endDate,
        bool isActive = true
    );
    Task<string> UpdateEventAsync(
        int eventId,
        string? name,
        string? description,
        decimal? discountPercent,
        string? colorTheme,
        string? bannerUrl,
        DateTime? startDate,
        DateTime? endDate,
        bool? isActive,
        string action = "update"
    );
    Task<string> AssignProductToEventAsync(int productId, int? eventId);
}
