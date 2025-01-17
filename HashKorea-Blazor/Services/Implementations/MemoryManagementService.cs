using Amazon.Runtime.Internal.Util;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace HashKorea.Services;

public class MemoryManagementService : IMemoryManagementService
{
    private readonly IDistributedCache _cache;

    public MemoryManagementService(IDistributedCache cache)
    {
        _cache = cache;
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? absoluteExpiration = null)
    {
        var options = new DistributedCacheEntryOptions();

        if (absoluteExpiration.HasValue)
        {   
            options.AbsoluteExpirationRelativeToNow = absoluteExpiration;
        }

        var serializedValue = JsonSerializer.Serialize(value);
        await _cache.SetStringAsync(key, serializedValue, options);
        
    }

    public async Task<T?> GetAsync<T>(string key)
    {
        var serializedValue = await _cache.GetStringAsync(key);

        if (string.IsNullOrEmpty(serializedValue))
        {
            return default;
        }

        return JsonSerializer.Deserialize<T>(serializedValue);
    }
}