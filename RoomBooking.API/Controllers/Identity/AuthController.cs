using Microsoft.AspNetCore.Mvc;
using RoomBooking.API.Controllers.Base;
using RoomBooking.Application.Abstractions.Services;
using RoomBooking.Application.DTOs.Auth;
using RoomBooking.Domain.Enums;
using System.Net;

namespace RoomBooking.API.Controllers.Identity
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : BaseApiController
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }
        [HttpPost("register")]
        public async Task<IActionResult> RegisterAsync([FromBody] RegisterModel registerModel)
        {
            var result = await _authService.RegisterAsync(registerModel);

            if(result.Succeeded == true)
            {
                return Ok( result.Data);
            }

            return MapError(result.ErrorType, result.ErrorMessage);
        }

        [HttpPost("login")]
        public async Task<IActionResult> LoginAsync([FromBody] LoginModel loginModel)
        {
            var result = await _authService.LoginAsync(loginModel);

            if (result.Succeeded == true)
            {
                return Ok(result.Data);
            }

            return MapError(result.ErrorType, result.ErrorMessage);
        }
    }
}
