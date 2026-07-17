using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RoomBooking.API.Controllers.Base;
using RoomBooking.Application.Abstractions.Services;
using RoomBooking.Application.DTOs.Room;

namespace RoomBooking.API.Controllers.Business
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class RoomController : BaseApiController
    {
        private readonly IRoomService _roomService;

        public RoomController(IRoomService roomService)
        {
            _roomService = roomService;
        }
        
        [HttpGet("{id}", Name = nameof(RoomController) + "." + nameof(GetByIdAsync))]
        public async Task<IActionResult> GetByIdAsync(Guid id)
        {
            var result = await _roomService.GetByIdAsync(id);

            if (result.Succeeded == true)
            {
                return Ok(result.Data);
            }

            return MapError(result.ErrorType, result.ErrorMessage);
        }

        [HttpGet]
        public async Task<IActionResult> GetAllAsync()
        {
            var result = await _roomService.GetAllAsync();

            if (result.Succeeded)
            {
                return Ok(result.Data);
            }

            return MapError(result.ErrorType, result.ErrorMessage);
        }
        [Authorize(Roles="Admin")]
        [HttpGet]
        public async Task<IActionResult> GetDeactivatedAsync()
        {
            var result = await _roomService.GetDeactivatedAsync();

            if (result.Succeeded)
            {
                return Ok(result.Data);
            }

            return MapError(result.ErrorType, result.ErrorMessage);
        }
        [HttpGet("{id}/availability")]
        public async Task<IActionResult> GetAvailabilityAsync(
                  Guid id, [FromQuery] DateTime dateFrom, [FromQuery] DateTime dateTo)
        {
            var result = await _roomService.GetAvailabilityAsync(id, dateFrom, dateTo);

            if (result.Succeeded)
            {
                return Ok(result.Data);
            }

            return MapError(result.ErrorType, result.ErrorMessage);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateAsync([FromBody] RoomInputModel model)
        {
            var result = await _roomService.CreateAsync(model);

            if (result.Succeeded)
            {
                return CreatedAtRoute(nameof(RoomController) + "." + nameof(GetByIdAsync), new { id = result.Data.Id }, result.Data);
            }

            return MapError(result.ErrorType, result.ErrorMessage);
        }
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateAsync(Guid id,[FromBody] RoomInputModel model)
        {
            var result = await _roomService.UpdateAsync(id,model);

            if (result.Succeeded)
            {
                return Ok(result.Data);
            }

            return MapError(result.ErrorType, result.ErrorMessage);
        }
        [Authorize(Roles = "Admin")]
        [HttpPatch("{id}")]
        public async Task<IActionResult> SetStatusAsync(Guid id, [FromQuery] bool isActive)
        {
            var result = await _roomService.SetStatusAsync(id, isActive);

            if (result.Succeeded)
            {
                return NoContent();
            }

            return MapError(result.ErrorType, result.ErrorMessage);
        }
    }
}
