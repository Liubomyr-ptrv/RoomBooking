using RoomBooking.Application.DTOs.Auth;

namespace RoomBooking.Application.Abstractions.Services
{
    public interface IAuthService
    {
        public Task<string> Register(RegisterDto dto);
        public Task<string> Login(LoginDto dto);
    }
}
