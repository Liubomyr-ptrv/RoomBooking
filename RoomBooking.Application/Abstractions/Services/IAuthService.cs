using RoomBooking.Application.Common;
using RoomBooking.Application.DTOs.Auth;

namespace RoomBooking.Application.Abstractions.Services
{
    public interface IAuthService
    {
        public Task<Result<string>> Register(RegisterModel dto);
        public Task<Result<string>> Login(LoginModel dto);
    }
}
