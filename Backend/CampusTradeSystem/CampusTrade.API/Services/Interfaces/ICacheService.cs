using System;
using System.Threading.Tasks;

namespace CampusTrade.API.Services.Interface
{
    /// <summary>
    /// 内存缓存服务接口
    /// </summary>
    public interface ICacheService
    {
        /// <summary>
        /// 获取或创建缓存项
        /// </summary>
        Task<T?> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null);

        /// <summary>
        /// 获取缓存值
        /// </summary>
        Task<T?> GetAsync<T>(string key);

        /// <summary>
        /// 批量获取多个键的缓存值
        /// </summary>
        Task<Dictionary<string, T>> GetAllAsync<T>(IEnumerable<string> keys);


        /// <summary>
        /// 设置缓存值
        /// </summary>
        Task SetAsync<T>(string key, T value, TimeSpan? expiration = null);

        /// <summary>
        /// 删除缓存项
        /// </summary>
        Task RemoveAsync(string key);

        /// <summary>
        /// 检查缓存是否存在
        /// </summary>
        Task<bool> ExistsAsync(string key);

        /// <summary>
        /// 按前缀批量删除
        /// </summary>
        Task RemoveByPrefixAsync(string prefix);

        /// <summary>
        /// 清空所有缓存
        /// </summary>
        Task ClearAllAsync();

        /// <summary>
        /// 获取缓存命中率
        /// </summary>
        Task RefreshAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null);
        Task<double> GetHitRate();
        Task<Dictionary<string, DateTime?>> GetExpirationInfo(params string[] keys);

        Task RemoveAllAsync(IEnumerable<string> keys);

        Task<IEnumerable<string>> GetKeysByPrefixAsync(string prefix);
    }
}