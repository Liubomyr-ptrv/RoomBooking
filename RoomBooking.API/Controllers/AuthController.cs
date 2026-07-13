using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RoomBooking.Application.Abstractions.Services;
using RoomBooking.Application.DTOs.Auth;
using RoomBooking.Domain.Enums;
using System.Net;

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
                return Ok( result.Data);
            }

            return MapError(result.ErrorType, result.ErrorMessage);
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginModel loginModel)
        {
            var result = await _authService.Login(loginModel);

            if (result.Succeeded == true)
            {
                return Ok(result.Data);
            }

            return MapError(result.ErrorType, result.ErrorMessage);
        }

        [Authorize(Roles = nameof(UserRole.Admin))]
        [HttpPost("assign-role")]
        public async Task<IActionResult> AssignRole([FromBody] AssignRoleModel model)
        {
            var result = await _authService.AssignRole(model);

            if (result.Succeeded == true)
            {
                return Ok(result.Data);
            }

            return MapError(result.ErrorType, result.ErrorMessage);
        }

        [Authorize(Roles = nameof(UserRole.Admin))]
        [HttpPost("remove-role")]
        public async Task<IActionResult> RemoveRole([FromBody] AssignRoleModel model)
        {
            var result = await _authService.RemoveRole(model);

            if (result.Succeeded == true)
            {
                return Ok(result.Data);
            }

            return MapError(result.ErrorType, result.ErrorMessage);
        }

        private IActionResult MapError(ExeptionType errorType, string? errorMessage)
        {
            return errorType switch
            {
                ExeptionType.NotFound => NotFound(errorMessage),
                ExeptionType.Conflict => Conflict(errorMessage),
                ExeptionType.Validation => BadRequest(errorMessage),
                ExeptionType.Forbidden => StatusCode((int)HttpStatusCode.Forbidden, errorMessage),
                ExeptionType.Unauthorized => Unauthorized(errorMessage),
                ExeptionType.InternalServerError => BadRequest(errorMessage),
                _ => BadRequest(errorMessage)
            };
        }

    }
}
