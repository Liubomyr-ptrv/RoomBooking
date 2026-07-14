using Microsoft.EntityFrameworkCore;
using RoomBooking.Application.Abstractions.Db;
using RoomBooking.Application.Abstractions.Services;
using RoomBooking.Application.Common;
using RoomBooking.Application.DTOs.Booking;
using RoomBooking.Domain.Entities;
using RoomBooking.Domain.Enums;

namespace RoomBooking.Application.Services
{
    public class BookingService : IBookingService
    {
        private readonly IAppDbContext _context;
        public BookingService(IAppDbContext context)
        {
            _context = context;
        }
        public async Task<Result<BookingModel>> GetByIdAsync(Guid bookingId, Guid userId)
        {
            var result = await _context.Bookings.AsNoTracking().FirstOrDefaultAsync(x => x.Id == bookingId && x.UserId == userId);

            if (result is null)
            {
                return Result<BookingModel>.Failure($"Бронювання з id {bookingId} не знайдено у даного користувача!", ExeptionType.NotFound);
            }

            return Result<BookingModel>.Success(MapToDto(result));
        }

        public async Task<Result<List<BookingModel>>> GetMyAsync(Guid userId)
        {
            var result = await _context.Bookings.AsNoTracking().Where(x => x.UserId == userId).ToListAsync();

            if (result == null || !result.Any())
            {

                return Result<List<BookingModel>>.Success(new List<BookingModel>());
            }

            return Result<List<BookingModel>>.Success(result.Select(MapToDto).ToList());

        }
        public async Task<Result<List<BookingModel>>> GetAllAsync()
        {
            var result = await _context.Bookings.AsNoTracking().ToListAsync();

            if (!result.Any())
            {
                return Result<List<BookingModel>>.Success(new List<BookingModel>());
            }

            return Result<List<BookingModel>>.Success(result.Select(MapToDto).ToList()); ;
        }
        public async Task<Result<BookingModel>> CreateBookingAsync(CreateBookingModel model, Guid userId)
        {
            if (model.StartTime >= model.EndTime)
            {
                return Result<BookingModel>.Failure("Час завершення має бути пізніше за час початку.", ExeptionType.Validation);
            }

            var room = await _context.Rooms.FirstOrDefaultAsync(x => x.Id == model.RoomId);

            if (room is null)
            {
                return Result<BookingModel>.Failure("Кімнату не знайдено", ExeptionType.NotFound);
            }

            var capacity = model.AttendeesCount <= room.Capacity;
            if (!capacity)
            {
                return Result<BookingModel>.Failure($"Забагато людей для цієї кімнати, її ємність {room.Capacity} людей", ExeptionType.Validation);
            }
            var durationInHours = (decimal)(model.EndTime - model.StartTime).TotalHours;
            var calculatedPrice = room.PricePerHouse * durationInHours * model.AttendeesCount;

            var booking = new Booking
            {
                RoomId = room.Id,
                UserId = userId,
                Id = Guid.NewGuid(),
                StartTime = model.StartTime,
                EndTime = model.EndTime,
                Title = model.Title,
                AttendeesCount = model.AttendeesCount,
                TotalPrice = calculatedPrice,
                Notes = model.Notes,
                Status = BookingStatus.Confirmed,
            };

            _context.Bookings.Add(booking);
            await _context.SaveChangesAsync();

            return Result<BookingModel>.Success(MapToDto(booking));
        }
        public async Task<Result<bool>> CancelBookingAsync(Guid bookingId, Guid userId)
        {
            var result = await _context.Bookings.FirstOrDefaultAsync(x => x.Id == bookingId && x.UserId == userId);

            if (result is null)
            {
                return Result<bool>.Failure($"Бронювання з id {bookingId} не знайдено у даного користувача!", ExeptionType.NotFound);
            }

            if (result.Status == BookingStatus.Cancelled)
                return Result<bool>.Failure("Бронювання вже скасоване.", ExeptionType.Conflict);

            result.Status = BookingStatus.Cancelled;
            await _context.SaveChangesAsync();

            return Result<bool>.Success(true);

        }
        private static BookingModel MapToDto(Booking booking)
        {
            return new BookingModel
            {
                Id = booking.Id,
                RoomId = booking.RoomId,
                StartTime = booking.StartTime,
                EndTime = booking.EndTime,
                Title = booking.Title,
                AttendeesCount = booking.AttendeesCount,
                TotalPrice = booking.TotalPrice,
                Notes = booking.Notes,
                Status = booking.Status,
            };
        }
    }
}
