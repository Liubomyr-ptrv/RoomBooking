using Microsoft.AspNetCore.Mvc;
using RoomBooking.Application.Abstractions.Services;
using RoomBooking.Application.DTOs.Auth;

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
        public async Task<IActionResult> Register([FromBody] RegisterDto registerDto)
        {
            try
            {
                return Ok(await _authService.Register(registerDto));
            }
            catch (InvalidOperationException ex) 
            { 
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
        {
            try
            {
                return Ok(await _authService.Login(loginDto));
            }
            catch (InvalidOperationException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
        }
    }
}
