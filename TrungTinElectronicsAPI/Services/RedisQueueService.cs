using StackExchange.Redis;
using Newtonsoft.Json;

namespace TrungTinElectronicsAPI.Services;

public class RedisQueueService
{
    private readonly IDatabase _db;
    private readonly ILogger<RedisQueueService> _logger;

    public RedisQueueService(IConnectionMultiplexer redis, ILogger<RedisQueueService> logger)
    {
        _db = redis.GetDatabase();
        _logger = logger;
    }

    // Đẩy job vào queue
    public async Task EnqueueAsync<T>(string queueName, T payload)
    {
        var json = JsonConvert.SerializeObject(payload);
        await _db.ListRightPushAsync(queueName, json);
        _logger.LogInformation("Enqueued job to {Queue}: {Payload}", queueName, json);
    }

    // Lấy job ra khỏi queue (blocking)
    public async Task<T?> DequeueAsync<T>(string queueName)
    {
        var value = await _db.ListLeftPopAsync(queueName);
        if (value.IsNullOrEmpty) return default;
        return JsonConvert.DeserializeObject<T>(value!);
    }

    // Idempotency check — tránh xử lý 2 lần
    public async Task<bool> AcquireLockAsync(string key, TimeSpan expiry)
    {
        return await _db.StringSetAsync(key, "1", expiry, When.NotExists);
    }

    // Cache revenue
    public async Task SetCacheAsync(string key, string value, TimeSpan expiry)
        => await _db.StringSetAsync(key, value, expiry);

    public async Task<string?> GetCacheAsync(string key)
    {
        var value = await _db.StringGetAsync(key);
        return value.IsNullOrEmpty ? null : value.ToString();
    }

    public async Task DeleteCacheAsync(string key)
        => await _db.KeyDeleteAsync(key);
}