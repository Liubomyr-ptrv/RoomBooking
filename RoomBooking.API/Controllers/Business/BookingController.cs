using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RoomBooking.API.Controllers.Base;
using RoomBooking.Application.Abstractions.Services;
using RoomBooking.Application.Common;
using RoomBooking.Application.DTOs.Booking;
using RoomBooking.Domain.Enums;
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
        [HttpGet("{bookingId}", Name = nameof(BookingController) + "_" + nameof(GetByIdAsync))]
        public async Task<IActionResult> GetByIdAsync(Guid bookingId)
        {
            var userId = GetUserId();
            if (!userId.Succeeded)
            {
                return MapError(userId.ErrorType, userId.ErrorMessage);
            }

            var result = await _bookingService.GetByIdAsync(bookingId, userId.Data);

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
            if (!userId.Succeeded)
            {
                return MapError(userId.ErrorType, userId.ErrorMessage);
            }
            var result = await _bookingService.GetByUserIdAsync(userId.Data);

            if (result.Succeeded)
            {
                return Ok(result.Data);
            }

            return MapError(result.ErrorType, result.ErrorMessage);
        }
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> GetAllAsync([FromBody]BookingFilterModel? model)
        {    
            var result = await _bookingService.GetAllAsync(model);

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
            if (!userId.Succeeded)
            {
                return MapError(userId.ErrorType, userId.ErrorMessage);
            }
            var result = await _bookingService.CreateBookingAsync(model,userId.Data);

            if (result.Succeeded)
            {
                return CreatedAtRoute(nameof(BookingController) + "_" + nameof(GetByIdAsync), new { id = result.Data.Id }, result.Data);
            }

            return MapError(result.ErrorType, result.ErrorMessage);
        }
        [HttpPatch("{bookingId}")]
        public async Task<IActionResult> CancelAsync(Guid bookingId)
        {
            var userId = GetUserId();
            if (!userId.Succeeded)
            {
                return MapError(userId.ErrorType, userId.ErrorMessage);
            }
            var isAdmin = User.IsInRole("Admin");

            var result = await _bookingService.CancelBookingAsync(bookingId, userId.Data, isAdmin);

            if (result.Succeeded)
            {
                return NoContent();
            }

            return MapError(result.ErrorType, result.ErrorMessage);
        }
        private Result<Guid> GetUserId()
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
            {
                return Result<Guid>.Failure("Токен не містить ідентифікатора користувача.", ErrorType.Unauthorized);
            }

            return Result<Guid>.Success(userId);
        }       
    }
}
