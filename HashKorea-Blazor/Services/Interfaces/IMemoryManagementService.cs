namespace HashKorea.Services;

public interface IMemoryManagementService
{
    Task SetAsync<T>(string key, T value, TimeSpan? absoluteExpiration = null);
    Task<T?> GetAsync<T>(string key);
}
