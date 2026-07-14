using RoomBooking.Application.Common;
using RoomBooking.Application.DTOs.Auth;

namespace RoomBooking.Application.Abstractions.Services
{
    public interface IAuthService
    {
        public Task<Result<string>> RegisterAsync(RegisterModel model);
        public Task<Result<string>> LoginAsync(LoginModel model);
    }
}
