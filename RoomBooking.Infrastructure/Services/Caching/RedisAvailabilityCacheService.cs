using Microsoft.Extensions.Caching.Distributed;
using RoomBooking.Application.Abstractions.Services;
using RoomBooking.Application.DTOs.Room;
using System.Text.Json;

namespace RoomBooking.Infrastructure.Services.Caching
{
    public class RedisAvailabilityCacheService : IAvailabilityCacheService
    {
        private readonly IDistributedCache _cache;

        public RedisAvailabilityCacheService(IDistributedCache cache)
        {
            _cache = cache;
        }

        public async Task<List<TimeSlotModel>?> GetAsync(Guid roomId )
        {
            try
            {
                var cached = await _cache.GetStringAsync(GetCacheKey(roomId));
                return cached is null ? null : JsonSerializer.Deserialize<List<TimeSlotModel>>(cached);
            }
            catch
            {
                return null;
            }
        }
        public async Task SetAsync(Guid roomId, List<TimeSlotModel> slots, TimeSpan ttl)
        {
            try
            {
                await _cache.SetStringAsync(
                    GetCacheKey(roomId),
                    JsonSerializer.Serialize(slots),
                    new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = ttl });
        
           }
            catch
            {
              
            }
        }
        public async Task InvalidateAsync(Guid roomId)
        {
            try
            {
               await _cache.RemoveAsync(GetCacheKey(roomId));
            }
            catch
            {
                
            }
        }
        private static string GetCacheKey(Guid roomId) => $"room-bookings:{roomId}";
    }
}
