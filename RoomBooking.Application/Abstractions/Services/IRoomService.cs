using RoomBooking.Application.Common;
using RoomBooking.Application.DTOs.Room;

namespace RoomBooking.Application.Abstractions.Services
{
    public interface IRoomService
    {
        public Task<Result<RoomModel>> GetByIdAsync(Guid id);
        public Task<Result<List<RoomListModel>>> GetAllAsync();
        public Task<Result<List<TimeSlotModel>>> GetAvailabilityAsync(Guid roomId, DateTime dateFrom, DateTime dateTo);
        public Task<Result<RoomModel>> CreateAsync(RoomInputModel model);
        public Task<Result<RoomModel>> UpdateAsync(Guid id,RoomInputModel model);
        public Task<Result<bool>> DeleteAsync(Guid id);
        public Task InvalidateAvailabilityCacheAsync(Guid roomId);
    }
}
