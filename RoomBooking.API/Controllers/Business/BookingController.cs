using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RoomBooking.API.Controllers.Base;
using RoomBooking.Application.Abstractions.Services;
using RoomBooking.Application.DTOs.Booking;
using System.Security.Claims;

namespace RoomBooking.API.Controllers.Business
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class BookingController : BaseApiController
    {
        private readonly IBookingService _bookingService;
        public BookingController(IBookingService bookingService)
        {
            _bookingService = bookingService;
        }
        [HttpGet("{bookingId}")]
        public async Task<IActionResult> GetByIdAsync(Guid bookingId)
        {
            var userId = GetUserId();
            var result = await _bookingService.GetByIdAsync(bookingId, userId);

            if (result.Succeeded)
            {
                return Ok(result.Data);
            }

            return MapError(result.ErrorType,result.ErrorMessage);
        }
        [HttpGet("my")]
        public async Task<IActionResult> GetMyAsync()
        {
            var userId = GetUserId();
            var result = await _bookingService.GetByUserIdAsync(userId);

            if (result.Succeeded)
            {
                return Ok(result.Data);
            }

            return MapError(result.ErrorType, result.ErrorMessage);
        }
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> GetAllAsync()
        {    
            var result = await _bookingService.GetAllAsync();

            if (result.Succeeded)
            {
                return Ok(result.Data);
            }

            return MapError(result.ErrorType, result.ErrorMessage);
        }
        [HttpPost]
        public async Task<IActionResult> CreateAsync([FromBody] CreateBookingModel model)
        {
            var userId = GetUserId();
            var result = await _bookingService.CreateBookingAsync(model,userId);

            if (result.Succeeded)
            {
                return CreatedAtAction(nameof(GetByIdAsync), new { bookingId = result.Data.Id }, result.Data);
            }

            return MapError(result.ErrorType, result.ErrorMessage);
        }
        [HttpPatch("{bookingId}")]
        public async Task<IActionResult> CancelAsync(Guid bookingId)
        {
            var userId = GetUserId();
            var result = await _bookingService.CancelBookingAsync(bookingId, userId);

            if (result.Succeeded)
            {
                return NoContent();
            }

            return MapError(result.ErrorType, result.ErrorMessage);
        }
        private Guid GetUserId()
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdString))
                throw new UnauthorizedAccessException("Токен не містить ідентифікатора користувача.");

            return Guid.Parse(userIdString);
        }       
    }
}
