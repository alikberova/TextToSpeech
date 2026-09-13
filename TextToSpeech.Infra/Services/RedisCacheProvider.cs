using StackExchange.Redis;
using System.Text.Json;
using TextToSpeech.Infra.Interfaces;

namespace TextToSpeech.Infra.Services;

public sealed class RedisCacheProvider(IConnectionMultiplexer redisConnection) : IRedisCacheProvider
{
    private static readonly TimeSpan DefaultExpiry = TimeSpan.FromDays(7);

    private readonly IDatabase _db = redisConnection.GetDatabase();

    public async Task<T?> Get<T>(string key)
    {
        var cachedData = await _db.StringGetAsync(key);

        if (!cachedData.IsNullOrEmpty)
        {
            return JsonSerializer.Deserialize<T>((string)cachedData!);
        }

        return default;
    }

    public async Task<byte[]?> GetBytes(string key)
    {
        var value = await _db.StringGetAsync(key);

        if (value.IsNullOrEmpty)
        {
            return null;
        }

        return (byte[])value!;
    }

    public Task Set<T>(string key, T data, TimeSpan? expiry = null)
    {
        var serializedData = JsonSerializer.Serialize(data);

        return _db.StringSetAsync(key, serializedData, expiry ?? DefaultExpiry);
    }

    public Task SetBytes(string key, byte[] data, TimeSpan? expiry = null)
    {
        return _db.StringSetAsync(key, data, expiry ?? DefaultExpiry);
    }
}
