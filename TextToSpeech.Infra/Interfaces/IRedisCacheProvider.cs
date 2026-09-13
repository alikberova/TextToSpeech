namespace TextToSpeech.Infra.Interfaces;

public interface IRedisCacheProvider
{
    Task<T?> Get<T>(string key);

    Task<byte[]?> GetBytes(string key);

    /// <summary>
    /// Serializes and stores data in Redis under the specified key.
    /// </summary>
    /// <param name="expiry">
    /// Optional expiration time. If <c>null</c>, a default expiration is used.
    /// </param>
    Task Set<T>(string key, T data, TimeSpan? expiry = null);

    /// <summary>
    /// Stores raw bytes in Redis under the specified key.
    /// </summary>
    /// <param name="expiry">
    /// Optional expiration time. If <c>null</c>, a default expiration is used.
    /// </param>
    Task SetBytes(string key, byte[] data, TimeSpan? expiry = null);
}