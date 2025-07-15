using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using System;
using System.Collections;
using System.Reflection;
using System.Threading.Tasks;
using CampusTrade.API.Options;

namespace CampusTrade.API.Services.Cache
{
    /// <summary>
    /// 内存缓存服务实现
    /// </summary>
    public class CacheService : ICacheService
    {
        private readonly IMemoryCache _memoryCache;
        private readonly CacheOptions _options;
        private readonly ILogger<CacheService> _logger;
        
        // 缓存命中统计（线程安全）
        private long _totalRequests = 0;
        private long _hits = 0;

        public CacheService(
            IMemoryCache memoryCache,
            IOptions<CacheOptions> options,
            ILogger<CacheService> logger)
        {
            _memoryCache = memoryCache;
            _options = options.Value;
            _logger = logger;
        }

       public async Task<T?> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null){
            Interlocked.Increment(ref _totalRequests);
            // 1. 检查内存缓存
            if (_memoryCache.TryGetValue(key, out T? cachedValue)) // 添加 ? 允许null
            {
                Interlocked.Increment(ref _hits);
                return cachedValue; // 允许返回null
            }

            // 2. 调用工厂方法
             var result = await factory().ConfigureAwait(false);
    
            // 3. 写入缓存（即使为null也缓存，防止缓存穿透）
             await SetAsync(key, result, expiration ?? _options.NullResultCacheDuration);
    
            return result;  // 允许返回null
        }

        public Task<T?> GetAsync<T>(string key)
        {
            Interlocked.Increment(ref _totalRequests);
            
            if (_memoryCache.TryGetValue(key, out T value))
            {
                Interlocked.Increment(ref _hits);
                return Task.FromResult<T?>(value);
            }
            
            return Task.FromResult<T?>(default);
        }

        public Task SetAsync<T>(string key, T value, TimeSpan? expiration = null)
        {
            var options = new MemoryCacheEntryOptions
            {
                Size = 1,
                AbsoluteExpirationRelativeToNow = expiration ?? _options.DefaultCacheDuration
            };
            
            _memoryCache.Set(key, value, options);
            return Task.CompletedTask;
        }

        public Task RemoveAsync(string key)
        {
            _memoryCache.Remove(key);
            return Task.CompletedTask;
        }

        public Task<bool> ExistsAsync(string key)
        {
            return Task.FromResult(_memoryCache.TryGetValue(key, out _));
        }

        public Task RemoveByPrefixAsync(string prefix)
        {
            if (_memoryCache is MemoryCache memoryCache)
            {
                var keys = MemoryCacheExtensions.GetKeys<string>(memoryCache)
                    .Where(k => k.StartsWith(prefix));
                
                foreach (var key in keys)
                {
                    _memoryCache.Remove(key);
                }
            }
            return Task.CompletedTask;
        }

        public Task ClearAllAsync()
        {
            if (_memoryCache is MemoryCache memoryCache)
            {
                memoryCache.Compact(1.0); // 压缩100% = 清空
            }
            return Task.CompletedTask;
        }

        public double GetHitRate()
        {
            return _totalRequests == 0 ? 0 : (double)_hits / _totalRequests;
        }
    }

    /// <summary>
    /// MemoryCache扩展方法（用于获取所有Key）
    /// </summary>
    internal static class MemoryCacheExtensions
    {
        private static readonly FieldInfo _entriesField = 
            typeof(MemoryCache).GetField("_entries", BindingFlags.NonPublic | BindingFlags.Instance)!;

        public static IEnumerable<T> GetKeys<T>(this IMemoryCache memoryCache)
        {
            if (_entriesField?.GetValue(memoryCache) is not IDictionary cacheEntries)
                yield break;

            foreach (DictionaryEntry entry in cacheEntries)
            {
                yield return (T)entry.Key;
            }
        }
    }
}