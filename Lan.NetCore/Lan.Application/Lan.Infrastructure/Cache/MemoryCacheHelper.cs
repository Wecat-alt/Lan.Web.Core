using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lan.Infrastructure.Cache
{
    public static class MemoryCacheHelper
    {
        private static readonly MemoryCache _cache = new(new MemoryCacheOptions());

        public static T? Get<T>(string key) => _cache.Get<T>(key);

        public static T GetOrCreate<T>(string key, Func<ICacheEntry, T> factory)
        {
            return _cache.GetOrCreate(key, factory)!;
        }

        public static T GetOrCreate<T>(string key, TimeSpan expiration, Func<T> factory)
        {
            return _cache.GetOrCreate(key, entry =>
            {
                entry.SetAbsoluteExpiration(expiration);
                return factory();
            })!;
        }

        public static void Set<T>(string key, T value, TimeSpan expiration)
        {
            _cache.Set(key, value, new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiration
            });
        }

        public static void Set<T>(string key, T value) => _cache.Set(key, value);

        public static void Remove(string key) => _cache.Remove(key);

        public static bool Exists(string key) => _cache.TryGetValue(key, out _);

        //使用方法
        // 设置缓存（永不过期）
        //MemoryCacheHelper.Set("app_name", "My Application");

        //// 设置带过期时间的缓存（5分钟后过期）
        //MemoryCacheHelper.Set("user_session", userSession, TimeSpan.FromMinutes(5));

        //// 获取缓存
        //var appName = MemoryCacheHelper.Get<string>("app_name");
        //        var session = MemoryCacheHelper.Get<UserSession>("user_session");

        //// 检查缓存是否存在
        //if (MemoryCacheHelper.Exists("user_session"))
        //{
        //    // 执行某些操作
        //}

        //// 删除缓存
        //MemoryCacheHelper.Remove("user_session");


        //gao 高级使用 - GetOrCreate 模式
        // 获取或创建缓存（如果不存在则执行工厂方法创建）
        //var expensiveData = MemoryCacheHelper.GetOrCreate("expensive_data", TimeSpan.FromHours(1), () =>
        //{
        //    // 这个函数只在缓存不存在时执行
        //    return CalculateExpensiveData(); // 执行耗时操作
        //});

        //        // 使用更复杂的工厂方法
        //        var complexData = MemoryCacheHelper.GetOrCreate("complex_data", entry =>
        //        {
        //            // 可以设置更详细的缓存选项
        //            entry.SetAbsoluteExpiration(TimeSpan.FromMinutes(30));
        //            entry.SetSlidingExpiration(TimeSpan.FromMinutes(10));

        //            return BuildComplexData(); // 执行复杂的数据构建
        //        });
    }
}