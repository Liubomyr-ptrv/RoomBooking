using Microsoft.AspNetCore.Mvc;
using RoomBooking.Application.Abstractions.Services;
using RoomBooking.Application.DTOs.Auth;
using RoomBooking.Domain.Enums;

namespace RoomBooking.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterModel registerModel)
        {
            var result = await _authService.Register(registerModel);

            if(result.Succeeded == true)
            {
                return Ok(new {token = result.Data });
            }

            return result.ErrorType switch
            {
                ExeptionType.UserAlreadyExists => Conflict(new { error = result.ErrorMessage }),
                ExeptionType.WeakPassword => BadRequest(new { error = result.ErrorMessage }),
                ExeptionType.InvalidEmailFormat => BadRequest(new { error = result.ErrorMessage }),
                _ => BadRequest(new { error = result.ErrorMessage })
            };
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginModel loginModel)
        {
            var result = await _authService.Login(loginModel);

            if (result.Succeeded == true)
            {
                return Ok(new { token = result.Data });
            }

            return result.ErrorType switch
            {
                ExeptionType.UserNotFound => NotFound(new { error = result.ErrorMessage }),
                ExeptionType.UserLockedOut => StatusCode(423, new { error = result.ErrorMessage }), 
                ExeptionType.InvalidEmailOrPassword => Unauthorized(new { error = result.ErrorMessage }),
                _ => BadRequest(new { error = result.ErrorMessage  })
            };
        }
    }
}
