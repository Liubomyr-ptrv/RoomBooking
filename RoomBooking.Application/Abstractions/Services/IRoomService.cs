using RoomBooking.Application.Common;
using RoomBooking.Application.DTOs.Room;
using System.Runtime.CompilerServices;

namespace RoomBooking.Application.Abstractions.Services
{
    public interface IRoomService
    {
        public Task<Result<RoomModel>> GetByIdAsync(Guid id);
        public Task<Result<List<RoomModel>>> GetAllAsync();
        public Task<Result<RoomModel>> CreateAsync(CreateRoomModel model);
        public Task<Result<RoomModel>> UpdateAsync(Guid id,UpdateRoomModel model);
        public Task<Result<bool>> DeleteAsync(Guid id);
    }
}
