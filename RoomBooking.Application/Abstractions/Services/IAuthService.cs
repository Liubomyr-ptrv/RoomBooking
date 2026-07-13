using RoomBooking.Application.Common;
using RoomBooking.Application.DTOs.Auth;

namespace RoomBooking.Application.Abstractions.Services
{
    public interface IAuthService
    {
        public Task<Result<string>> Register(RegisterModel model);
        public Task<Result<string>> Login(LoginModel model);
        public Task<Result<bool>> AssignRole(AssignRoleModel model);
        public Task<Result<bool>> RemoveRole(AssignRoleModel model);
    }
}
