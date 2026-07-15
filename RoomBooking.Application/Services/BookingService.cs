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
            var result = await _context.Bookings
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == bookingId && x.UserId == userId);

            if (result is null)
            {
                return Result<BookingModel>.Failure($"Бронювання з id {bookingId} не знайдено у даного користувача!", ExeptionType.NotFound);
            }

            return Result<BookingModel>.Success(MapToDto(result));
        }

        public async Task<Result<List<BookingModel>>> GetByUserIdAsync(Guid userId)
        {
            var result = await _context.Bookings
                .AsNoTracking()
                .Where(x => x.UserId == userId)
                .OrderBy(x => x.StartTime)
                .ToListAsync();

            return Result<List<BookingModel>>.Success(result.Select(MapToDto).ToList());

        }
        public async Task<Result<List<BookingModel>>> GetAllAsync()
        {
            var result = await _context.Bookings
                .AsNoTracking()
                .OrderBy(x => x.StartTime)
                .ToListAsync();

            return Result<List<BookingModel>>.Success(result.Select(MapToDto).ToList()); 
        }

        public async Task<Result<BookingModel>> CreateBookingAsync(CreateBookingModel model, Guid userId)
        {
            var validationError = BookingTimeValidation(model);
            if (validationError is not null)
            {
                return Result<BookingModel>.Failure(validationError, ExeptionType.Validation);
            }

            if (model.AttendeesCount <= 0)
            {
                return Result<BookingModel>.Failure("Кількість учасників має бути більшою за нуль.", ExeptionType.Validation);
            }

            var room = await _context.Rooms.FirstOrDefaultAsync(x => x.Id == model.RoomId);

            if (room is null)
            {
                return Result<BookingModel>.Failure("Кімнату не знайдено", ExeptionType.NotFound);
            }

            var roomValidation = ValidateRoom(room, model.AttendeesCount);
            if (!roomValidation.Succeeded)
            {
                return Result<BookingModel>.Failure(roomValidation.ErrorMessage!, roomValidation.ErrorType);
            }

            var hasOverlap = await HasOverlapAsync(model.RoomId, model.StartTime, model.EndTime);
            if (hasOverlap)
                return Result<BookingModel>.Failure("Кімната вже заброньована на цей час.", ExeptionType.Conflict);
   
            var durationInHours = (decimal)(model.EndTime - model.StartTime).TotalHours;
            var calculatedPrice = room.PricePerHouse * durationInHours;

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
            {
                return Result<bool>.Failure("Бронювання вже скасоване.", ExeptionType.Conflict);
            }

            if (result.StartTime <= DateTime.UtcNow)
            {
                return Result<bool>.Failure("Неможливо скасувати бронювання, яке вже розпочалося або завершилося.", ExeptionType.Conflict);
            }

            result.Status = BookingStatus.Cancelled;
            await _context.SaveChangesAsync();

            return Result<bool>.Success(true);

        }
        private async Task<bool> HasOverlapAsync(Guid roomId, DateTime startTime, DateTime endTime)
        {
            return await _context.Bookings.AnyAsync(b =>
                b.RoomId == roomId &&
                b.Status != BookingStatus.Cancelled &&
                startTime < b.EndTime &&
                endTime > b.StartTime);
        }
        private static Result<Room> ValidateRoom(Room? room, int attendeesCount)
        {
            if (room is null)
                return Result<Room>.Failure("Кімнату не знайдено", ExeptionType.NotFound);

            if (!room.IsActive)
                return Result<Room>.Failure("Ця кімната наразі недоступна для бронювання.", ExeptionType.Conflict);

            if (attendeesCount > room.Capacity)
                return Result<Room>.Failure($"Забагато людей для цієї кімнати, її ємність {room.Capacity} людей", ExeptionType.Validation);

            return Result<Room>.Success(room);
        }
        private static string? BookingTimeValidation(CreateBookingModel model)
        {
            if (model.StartTime < DateTime.UtcNow)
                return "Неможливо створити бронювання в минулому.";

            if (model.StartTime >= model.EndTime)
                return "Час завершення має бути пізніше за час початку.";

            return null;
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
