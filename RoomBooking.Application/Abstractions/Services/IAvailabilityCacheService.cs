using RoomBooking.Application.DTOs.Room;

namespace RoomBooking.Application.Abstractions.Services
{
    public interface IAvailabilityCacheService
    {
       public Task<List<TimeSlotModel>?> GetAsync(string cacheKey);
       public Task SetAsync(Guid roomId, string cacheKey, List<TimeSlotModel> slots, TimeSpan ttl);
       public Task InvalidateAsync(Guid roomId);
    }
}
