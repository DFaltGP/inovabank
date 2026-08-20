using InovaBank.Domain.Interfaces;
using StackExchange.Redis;
using System.Text.Json;

namespace InovaBank.Infrastructure.Services.Cache;

public sealed class RedisCacheService(IConnectionMultiplexer redis) : ICacheService
{
    private readonly IDatabase _db = redis.GetDatabase();

    public async Task<T?> GetAsync<T>(string key, CancellationToken ct = default)
    {
        var data = await _db.StringGetAsync(key);
        return data.IsNullOrEmpty ? default : JsonSerializer.Deserialize<T>((string)data!);
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken ct = default)
    {
        var data = JsonSerializer.Serialize(value);
        await _db.StringSetAsync(key, data, expiration ?? TimeSpan.FromHours(24));
    }

    public async Task<bool> SetIfNotExistsAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken ct = default)
    {
        var data = JsonSerializer.Serialize(value);
        return await _db.StringSetAsync(key, data, expiration ?? TimeSpan.FromMinutes(2), When.NotExists);
    }

    public async Task RemoveAsync(string key, CancellationToken ct = default) =>
        await _db.KeyDeleteAsync(key);
}