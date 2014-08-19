using System;
using System.Collections.Specialized;
using System.Globalization;
using System.Runtime.Caching;

namespace WintermintClient.JsApi
{
    internal class CacheHelper
    {
        private const int kCacheMemoryLimitMegabytes = 10;

        private const int kPhysicalMemoryLimitPercentage = 5;

        private MemoryCache cache;

        private CacheItemPolicy defaultCachePolicy;

        private CacheItemPolicy fiveMinuteCachePolicy;

        private CacheItemPolicy permanentCachePolicy;

        public int ItemCount
        {
            get
            {
                return (int)this.cache.GetCount(null);
            }
        }

        public CacheHelper()
        {
            NameValueCollection nameValueCollection = new NameValueCollection()
            {
                { "CacheMemoryLimitMegabytes", 10.ToString(CultureInfo.InvariantCulture) },
                { "PhysicalMemoryLimitPercentage", 5.ToString(CultureInfo.InvariantCulture) }
            };
            this.cache = new MemoryCache("WintermintJsApiCache", nameValueCollection);
            this.defaultCachePolicy = new CacheItemPolicy();
            CacheItemPolicy cacheItemPolicy = new CacheItemPolicy()
            {
                SlidingExpiration = TimeSpan.FromMinutes(5)
            };
            this.fiveMinuteCachePolicy = cacheItemPolicy;
            this.permanentCachePolicy = new CacheItemPolicy()
            {
                Priority = CacheItemPriority.NotRemovable
            };
        }

        public T Get<T>(string key)
        {
            return (T)this.cache.Get(key, null);
        }

        public object Get(string key)
        {
            return this.cache.Get(key, null);
        }

        public object Remove(string key)
        {
            return this.cache.Remove(key, null);
        }

        public void Set(string key, object value)
        {
            this.cache.Set(key, value, this.defaultCachePolicy, null);
        }

        public void SetCustom(string key, object value, CacheItemPolicy cachePolicy)
        {
            this.cache.Set(key, value, cachePolicy, null);
        }

        public void SetExpiring(string key, object value)
        {
            this.cache.Set(key, value, this.fiveMinuteCachePolicy, null);
        }

        public void SetPermanent(string key, object value)
        {
            this.cache.Set(key, value, this.permanentCachePolicy, null);
        }
    }
}