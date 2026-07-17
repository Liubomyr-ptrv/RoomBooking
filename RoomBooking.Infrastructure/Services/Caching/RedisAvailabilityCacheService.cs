using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using RoomBooking.Application.Abstractions.Services;
using RoomBooking.Application.DTOs.Room;
using StackExchange.Redis;
using System.Text.Json;

namespace RoomBooking.Infrastructure.Services.Caching
{
    public class RedisAvailabilityCacheService : IAvailabilityCacheService
    {
        private readonly IDistributedCache _cache;
        private readonly IConnectionMultiplexer _redis;
        private readonly ILogger<RedisAvailabilityCacheService> _logger;

        public RedisAvailabilityCacheService(IDistributedCache cache, IConnectionMultiplexer redis, ILogger<RedisAvailabilityCacheService> logger)
        {
            _cache = cache;
            _redis = redis;
            _logger = logger;
        }

        public async Task<List<TimeSlotModel>?> GetAsync(string cacheKey)
        {
            try
            {
                var cached = await _cache.GetStringAsync(cacheKey);
                return cached is null ? null : JsonSerializer.Deserialize<List<TimeSlotModel>>(cached);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Не вдалося отримати дані з кешу за ключем {CacheKey}. Виконується Fallback до бази даних", cacheKey);
                return null;
            }
        }
        public async Task SetAsync(Guid roomId, string cacheKey, List<TimeSlotModel> slots, TimeSpan ttl)
        {
            try
            {
                await _cache.SetStringAsync(
                    cacheKey,
                    JsonSerializer.Serialize(slots),
                    new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = ttl });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Не вдалося зберегти дані в кеш для кімнати {RoomId} за ключем {CacheKey}", roomId, cacheKey);
            }
        }
        public async Task InvalidateAsync(Guid roomId)
        {
            try
            {
                var pattern = $"room-availability:{roomId}:*";
                var db = _redis.GetDatabase();

                foreach (var endpoint in _redis.GetEndPoints())
                {
                    var server = _redis.GetServer(endpoint);

                    if (server.IsReplica)
                        continue;

                    var keysToDelete = new List<RedisKey>();

                    await foreach (var key in server.KeysAsync(pattern: pattern))
                    {
                        keysToDelete.Add(key);
                    }

                    if (keysToDelete.Count > 0)
                    {
                        await db.KeyDeleteAsync(keysToDelete.ToArray());
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Помилка при інвалідації кешу для кімнати {RoomId}.", roomId);
            }
        }
    }
}
