using Microsoft.AspNetCore.Mvc;
using RoomBooking.Application.Abstractions.Services;
using RoomBooking.Application.DTOs.Auth;
using RoomBooking.Domain.Enums;
using System.Net;

namespace RoomBooking.API.Controllers.Identity
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
                return Ok( result.Data);
            }

            if (result.ErrorType == ExeptionType.Conflict)
                return Conflict(result.ErrorMessage);
            if (result.ErrorType == ExeptionType.Validation)
                return BadRequest(result.ErrorMessage);

            return BadRequest();
        
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginModel loginModel)
        {
            var result = await _authService.Login(loginModel);

            if (result.Succeeded == true)
            {
                return Ok(result.Data);
            }

            if (result.ErrorType == ExeptionType.NotFound)
                return NotFound(result.ErrorMessage);

            if (result.ErrorType == ExeptionType.Forbidden)
                return StatusCode((int)HttpStatusCode.Forbidden, result.ErrorMessage);

            if (result.ErrorType == ExeptionType.Unauthorized)
                return Unauthorized(result.ErrorMessage);

            return BadRequest();
        }
    }
}
