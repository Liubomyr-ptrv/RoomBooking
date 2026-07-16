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
        
        [HttpGet("{id}")]
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

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateAsync([FromBody] RoomInputModel model)
        {
            var result = await _roomService.CreateAsync(model);

            if (result.Succeeded)
            {
                return CreatedAtAction("GetById", new { id = result.Data.Id }, result.Data);
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

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteAsync(Guid id)
        {
            var result = await _roomService.DeleteAsync(id);

            if (result.Succeeded)
                return NoContent();

            return MapError(result.ErrorType, result.ErrorMessage);
        }   
    }
}
