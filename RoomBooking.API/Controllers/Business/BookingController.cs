using Microsoft.AspNetCore.Mvc;
using RoomBooking.Application.Abstractions.Services;
using RoomBooking.Application.DTOs.Booking;
using RoomBooking.Domain.Enums;
using System.Net;
using System.Security.Claims;

namespace RoomBooking.API.Controllers.Business
{
    [Route("api/[controller]")]
    [ApiController]
    public class BookingController : ControllerBase
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
                return CreatedAtAction(nameof(GetByIdAsync), result.Data);
            }

            return MapError(result.ErrorType, result.ErrorMessage);
        }
        [HttpPut("{bookingId}")]
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
        private IActionResult MapError(ExeptionType errorType, string? errorMessage)
        {
            return errorType switch
            {
                ExeptionType.NotFound => NotFound(errorMessage),
                ExeptionType.Conflict => Conflict(errorMessage),
                ExeptionType.Validation => BadRequest(errorMessage),
                ExeptionType.Forbidden => StatusCode((int)HttpStatusCode.Forbidden, errorMessage),
                ExeptionType.Unauthorized => Unauthorized(errorMessage),
                ExeptionType.InternalServerError => StatusCode((int)HttpStatusCode.InternalServerError, errorMessage),
                _ => BadRequest(errorMessage)
            };
        }
    }
}
