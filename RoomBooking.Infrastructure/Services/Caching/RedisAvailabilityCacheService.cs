using Microsoft.Extensions.Caching.Distributed;
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
        private static readonly TimeSpan IndexTtlBuffer = TimeSpan.FromMinutes(5);

        public RedisAvailabilityCacheService(IDistributedCache cache, IConnectionMultiplexer redis)
        {
            _cache = cache;
            _redis = redis;
        }

        public async Task<List<TimeSlotModel>?> GetAsync(string cacheKey)
        {
            try
            {
                var cached = await _cache.GetStringAsync(cacheKey);
                return cached is null ? null : JsonSerializer.Deserialize<List<TimeSlotModel>>(cached);
            }
            catch
            {
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

                var db = _redis.GetDatabase();
                var indexKey = GetIndexKey(roomId);

                await db.SetAddAsync(indexKey, cacheKey);
                await db.KeyExpireAsync(indexKey, ttl + IndexTtlBuffer);
            }
            catch
            {
              
            }
        }
        public async Task InvalidateAsync(Guid roomId)
        {
            try
            {
                var indexKey = GetIndexKey(roomId);
                var db = _redis.GetDatabase();

                var keys = await db.SetMembersAsync(indexKey);
                if (keys.Length == 0)
                    return;

                var keysToDelete = keys
                   .Select(k => (RedisKey)k.ToString())
                   .Append((RedisKey)indexKey)
                   .ToArray();

                await db.KeyDeleteAsync(keysToDelete);
            }
            catch
            {
                
            }
        }
        private static string GetIndexKey(Guid roomId) => $"room-availability-keys:{roomId}";
    }
}
