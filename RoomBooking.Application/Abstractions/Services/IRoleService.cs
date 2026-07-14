using RoomBooking.Application.Common;
using RoomBooking.Application.DTOs.Auth;

namespace RoomBooking.Application.Abstractions.Services
{
    public interface IRoleService
    {
        public Task<Result<bool>> AssignRoleAsync(AssignRoleModel model);
        public Task<Result<bool>> RemoveRoleAsync(AssignRoleModel model);
    }
}
