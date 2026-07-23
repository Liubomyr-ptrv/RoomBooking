using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using RoomBooking.Application.Abstractions.Services;
using RoomBooking.Application.DTOs.Room;
using System.Text.Json;

namespace RoomBooking.Infrastructure.Services.Caching
{
    public class RedisAvailabilityCacheService : IAvailabilityCacheService
    {
        private readonly IDistributedCache _cache;
        private readonly ILogger<RedisAvailabilityCacheService> _logger;

        public RedisAvailabilityCacheService(IDistributedCache cache, ILogger<RedisAvailabilityCacheService> logger)
        {
            _cache = cache;
            _logger = logger;
        }

        public async Task<List<TimeSlotModel>?> GetAsync(Guid roomId )
        {
            var cacheKey = GetCacheKey(roomId);
            try
            {
                var cached = await _cache.GetStringAsync(GetCacheKey(roomId));
                return cached is null ? null : JsonSerializer.Deserialize<List<TimeSlotModel>>(cached);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to get cached data for key {CacheKey}. Falling back to database", cacheKey);
                return null;
            }
        }
        public async Task SetAsync(Guid roomId, List<TimeSlotModel> slots, TimeSpan ttl)
        {
            var cacheKey = GetCacheKey(roomId);
            try
            {
                var jitter = TimeSpan.FromSeconds(Random.Shared.Next(0, 31)); 
                var finalTtl = ttl.Add(jitter);

                await _cache.SetStringAsync(
                    GetCacheKey(roomId),
                    JsonSerializer.Serialize(slots),
                    new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = ttl });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to set cache for room {RoomId} with key {CacheKey}", roomId, cacheKey);
            }
        }
        public async Task InvalidateAsync(Guid roomId)
        {
            try
            {
               await _cache.RemoveAsync(GetCacheKey(roomId));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to invalidate cache for room {RoomId}", roomId);
            }
        }
        private static string GetCacheKey(Guid roomId) => $"room-bookings:{roomId}";
    }
}
