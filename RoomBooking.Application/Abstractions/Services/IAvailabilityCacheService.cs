using RoomBooking.Application.DTOs.Room;

namespace RoomBooking.Application.Abstractions.Services
{
    public interface IAvailabilityCacheService
    {
       public Task<List<TimeSlotModel>?> GetAsync(Guid roomId);
       public Task SetAsync(Guid roomId, List<TimeSlotModel> slots, TimeSpan ttl);
       public Task InvalidateAsync(Guid roomId);
    }
}
