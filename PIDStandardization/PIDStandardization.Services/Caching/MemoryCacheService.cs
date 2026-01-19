using Microsoft.Extensions.Caching.Memory;
using Serilog;

namespace PIDStandardization.Services.Caching
{
    /// <summary>
    /// Interface for caching service
    /// </summary>
    public interface ICacheService
    {
        T? Get<T>(string key);
        void Set<T>(string key, T value, TimeSpan? expiration = null);
        void Remove(string key);
        void RemoveByPrefix(string prefix);
        void Clear();
        bool TryGetValue<T>(string key, out T? value);
    }

    /// <summary>
    /// Memory cache service for caching frequently accessed data
    /// </summary>
    public class MemoryCacheService : ICacheService, IDisposable
    {
        private readonly IMemoryCache _cache;
        private readonly HashSet<string> _keys;
        private readonly object _keysLock = new object();
        private readonly TimeSpan _defaultExpiration;
        private bool _disposed;

        // Cache key prefixes for different entity types
        public static class CacheKeys
        {
            public const string Projects = "projects";
            public const string Equipment = "equipment";
            public const string Lines = "lines";
            public const string Instruments = "instruments";
            public const string Drawings = "drawings";
            public const string EquipmentTypes = "equipment_types";

            public static string ProjectById(Guid id) => $"{Projects}:{id}";
            public static string EquipmentByProject(Guid projectId) => $"{Equipment}:project:{projectId}";
            public static string EquipmentTagsByProject(Guid projectId) => $"{Equipment}:tags:{projectId}";
            public static string LinesByProject(Guid projectId) => $"{Lines}:project:{projectId}";
            public static string InstrumentsByProject(Guid projectId) => $"{Instruments}:project:{projectId}";
            public static string DrawingsByProject(Guid projectId) => $"{Drawings}:project:{projectId}";
        }

        /// <summary>
        /// Constructor with default settings
        /// </summary>
        public MemoryCacheService() : this(TimeSpan.FromMinutes(5))
        {
        }

        /// <summary>
        /// Constructor with custom default expiration
        /// </summary>
        public MemoryCacheService(TimeSpan defaultExpiration)
        {
            _cache = new MemoryCache(new MemoryCacheOptions
            {
                SizeLimit = 1000, // Limit cache to 1000 entries
                ExpirationScanFrequency = TimeSpan.FromMinutes(1)
            });
            _keys = new HashSet<string>();
            _defaultExpiration = defaultExpiration;

            Log.Debug("MemoryCacheService initialized with default expiration: {Expiration}", defaultExpiration);
        }

        /// <summary>
        /// Gets a value from cache
        /// </summary>
        public T? Get<T>(string key)
        {
            if (_cache.TryGetValue(key, out T? value))
            {
                Log.Debug("Cache hit for key: {Key}", key);
                return value;
            }

            Log.Debug("Cache miss for key: {Key}", key);
            return default;
        }

        /// <summary>
        /// Tries to get a value from cache
        /// </summary>
        public bool TryGetValue<T>(string key, out T? value)
        {
            var result = _cache.TryGetValue(key, out value);
            Log.Debug("Cache {Result} for key: {Key}", result ? "hit" : "miss", key);
            return result;
        }

        /// <summary>
        /// Sets a value in cache with optional expiration
        /// </summary>
        public void Set<T>(string key, T value, TimeSpan? expiration = null)
        {
            var actualExpiration = expiration ?? _defaultExpiration;

            var options = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(actualExpiration)
                .SetSize(1) // Each entry counts as 1 towards size limit
                .RegisterPostEvictionCallback((evictedKey, evictedValue, reason, state) =>
                {
                    Log.Debug("Cache entry evicted: {Key}, Reason: {Reason}", evictedKey, reason);
                    lock (_keysLock)
                    {
                        _keys.Remove(evictedKey.ToString()!);
                    }
                });

            _cache.Set(key, value, options);

            lock (_keysLock)
            {
                _keys.Add(key);
            }

            Log.Debug("Cache set for key: {Key}, Expiration: {Expiration}", key, actualExpiration);
        }

        /// <summary>
        /// Removes a specific key from cache
        /// </summary>
        public void Remove(string key)
        {
            _cache.Remove(key);

            lock (_keysLock)
            {
                _keys.Remove(key);
            }

            Log.Debug("Cache removed for key: {Key}", key);
        }

        /// <summary>
        /// Removes all keys with the specified prefix
        /// </summary>
        public void RemoveByPrefix(string prefix)
        {
            List<string> keysToRemove;

            lock (_keysLock)
            {
                keysToRemove = _keys.Where(k => k.StartsWith(prefix)).ToList();
            }

            foreach (var key in keysToRemove)
            {
                Remove(key);
            }

            Log.Debug("Cache removed {Count} entries with prefix: {Prefix}", keysToRemove.Count, prefix);
        }

        /// <summary>
        /// Clears all cache entries
        /// </summary>
        public void Clear()
        {
            List<string> allKeys;

            lock (_keysLock)
            {
                allKeys = _keys.ToList();
                _keys.Clear();
            }

            foreach (var key in allKeys)
            {
                _cache.Remove(key);
            }

            Log.Information("Cache cleared - removed {Count} entries", allKeys.Count);
        }

        /// <summary>
        /// Gets or sets a value using a factory function
        /// </summary>
        public async Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null)
        {
            if (_cache.TryGetValue(key, out T? cachedValue) && cachedValue != null)
            {
                Log.Debug("Cache hit for key: {Key}", key);
                return cachedValue;
            }

            Log.Debug("Cache miss for key: {Key}, executing factory", key);
            var value = await factory();
            Set(key, value, expiration);
            return value;
        }

        /// <summary>
        /// Gets cache statistics
        /// </summary>
        public (int EntryCount, IReadOnlyCollection<string> Keys) GetStatistics()
        {
            lock (_keysLock)
            {
                return (_keys.Count, _keys.ToList().AsReadOnly());
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _cache.Dispose();
                _disposed = true;
                Log.Debug("MemoryCacheService disposed");
            }
        }
    }
}
