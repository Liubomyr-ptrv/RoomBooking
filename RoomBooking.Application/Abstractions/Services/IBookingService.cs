using RoomBooking.Application.Common;
using RoomBooking.Application.DTOs.Booking;

namespace RoomBooking.Application.Abstractions.Services
{
    public interface IBookingService
    {
        public Task<Result<BookingModel>> GetByIdAsync(Guid bookingId, Guid userId);
        public Task<Result<List<BookingModel>>> GetByUserIdAsync(Guid userId);
        public Task<Result<List<BookingModel>>> GetAllAsync(BookingFilterModel? filter = null);
        public Task<Result<BookingModel>> CreateBookingAsync(CreateBookingModel model, Guid userId);
        public Task<Result<bool>> CancelBookingAsync(Guid bookingId, Guid userId, bool isAdmin);
    }
}
