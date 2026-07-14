using Microsoft.AspNetCore.Mvc;
using RoomBooking.Application.Abstractions.Services;
using RoomBooking.Application.DTOs.Room;
using RoomBooking.Domain.Enums;
using System.Net;

namespace RoomBooking.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RoomController : ControllerBase
    {
        private readonly IRoomService _roomService;

        public RoomController(IRoomService roomService)
        {
            _roomService = roomService;
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var result = await _roomService.GetByIdAsync(id);

            if (result.Succeeded == true)
            {
                return Ok(result.Data);
            }

            return MapError(result.ErrorType, result.ErrorMessage);
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var result = await _roomService.GetAllAsync();

            if (result.Succeeded == true)
            {
                return Ok(result.Data);
            }

            return MapError(result.ErrorType, result.ErrorMessage);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateRoomModel model)
        {
            var result = await _roomService.CreateAsync(model);

            if (result.Succeeded == true)
            {
                return CreatedAtAction(nameof(GetById),result.Data);
            }

            return MapError(result.ErrorType, result.ErrorMessage);
        }
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id,[FromBody] UpdateRoomModel model)
        {
            var result = await _roomService.UpdateAsync(id,model);

            if (result.Succeeded == true)
            {
                return Ok(result.Data);
            }

            return MapError(result.ErrorType, result.ErrorMessage);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var result = await _roomService.DeleteAsync(id);

            if (result.Succeeded == true)
                return NoContent();

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
