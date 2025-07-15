// Services/Interfaces/ICacheService.cs
public interface ICacheService
{
    // 基础操作
    Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null);
    Task<T> GetAsync<T>(string key);
    Task SetAsync<T>(string key, T value, TimeSpan? expiration = null);
    Task RemoveAsync(string key);
    Task<bool> ExistsAsync(string key);

    // 批量操作
    Task RemoveByPrefixAsync(string prefix);
    Task ClearAllAsync();

    // 监控
    Task<double> GetHitRate();
}