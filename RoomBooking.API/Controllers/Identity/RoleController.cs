using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RoomBooking.API.Controllers.Base;
using RoomBooking.Application.Abstractions.Services;
using RoomBooking.Application.DTOs.Auth;
using RoomBooking.Domain.Enums;

namespace RoomBooking.API.Controllers.Identity
{
    [Route("api/[controller]")]
    [ApiController]
    public class RoleController : BaseApiController
    {
        private readonly IRoleService _roleService;
        public RoleController(IRoleService roleService)
        {
            _roleService = roleService;
        }

        [Authorize(Roles = nameof(UserRole.Admin))]
        [HttpPost("assign-role")]
        public async Task<IActionResult> AssignRole([FromBody] AssignRoleModel model)
        {
            var result = await _roleService.AssignRoleAsync(model);

            if(result.Succeeded == true)
            {
                return Ok(result.Data);
            }

            return MapError(result.ErrorType, result.ErrorMessage);
        }

        [Authorize(Roles = nameof(UserRole.Admin))]
        [HttpPost("remove-role")]
        public async Task<IActionResult> RemoveRole([FromBody] AssignRoleModel model)
        {
            var result = await _roleService.RemoveRoleAsync(model);

            if (result.Succeeded == true)
            {
                return Ok(result.Data);
            }

            return MapError(result.ErrorType, result.ErrorMessage);
        }
    }
}
