using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RoomBooking.Application.Abstractions.Db;
using RoomBooking.Application.Abstractions.Services;
using RoomBooking.Application.Common;
using RoomBooking.Application.DTOs.Booking;
using RoomBooking.Domain.Entities;
using RoomBooking.Domain.Enums;
using System.Data;

namespace RoomBooking.Application.Services
{
    public class BookingService : IBookingService
    {
        private readonly IAppDbContext _context;
        private readonly IAvailabilityCacheService _cacheService;
        private static readonly TimeSpan MaxBookingDuration = TimeSpan.FromDays(7);
        private readonly ILogger<BookingService> _logger;
        public BookingService(IAppDbContext context, IRoomService roomService, ILogger<BookingService> logger)
        {
            _context = context;
            _roomService = roomService;
            _logger = logger;
        }
        public async Task<Result<BookingModel>> GetByIdAsync(Guid bookingId, Guid userId)
        {
            var result = await _context.Bookings
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == bookingId && x.UserId == userId);

            if (result is null)
            {
                return Result<BookingModel>.Failure($"Бронювання з id {bookingId} не знайдено у даного користувача!", ErrorType.NotFound);
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
        public async Task<Result<List<BookingModel>>> GetAllAsync(BookingFilterModel? filter)
        {
            var query = _context.Bookings.AsNoTracking().AsQueryable();

            if (filter is not null)
            {
                if (filter.RoomId.HasValue)
                    query = query.Where(x => x.RoomId == filter.RoomId.Value);

                if (filter.UserId.HasValue)
                    query = query.Where(x => x.UserId == filter.UserId.Value);

                if (filter.Status.HasValue)
                    query = query.Where(x => x.Status == filter.Status.Value);

                if (filter.FromDate.HasValue)
                    query = query.Where(x => x.StartTime >= filter.FromDate.Value);

                if (filter.ToDate.HasValue)
                    query = query.Where(x => x.EndTime <= filter.ToDate.Value);
            }

            var result = await query         
                .OrderBy(x => x.StartTime)
                .ToListAsync();

            return Result<List<BookingModel>>.Success(result.Select(MapToDto).ToList());
        }

        public async Task<Result<BookingModel>> CreateBookingAsync(CreateBookingModel model, Guid userId)
        {
            var validationError = BookingTimeValidation(model);
            if (validationError is not null)
            {
                return Result<BookingModel>.Failure(validationError, ErrorType.Validation);
            }

            if (model.AttendeesCount <= 0)
            {
                return Result<BookingModel>.Failure("Кількість учасників має бути більшою за нуль.", ErrorType.Validation);
            }

            var room = await _context.Rooms.AsNoTracking().FirstOrDefaultAsync(x => x.Id == model.RoomId);

            var roomValidation = ValidateRoom(room, model.AttendeesCount);
            if (!roomValidation.Succeeded)
            {
                return Result<BookingModel>.Failure(roomValidation.ErrorMessage!, roomValidation.ErrorType);
            }

            var durationInHours = (decimal)(model.EndTime - model.StartTime).TotalHours;
            var calculatedPrice = room.PricePerHour * durationInHours;

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

            await using var transaction = await _context.BeginTransactionAsync(IsolationLevel.Serializable);

            try
            {
                var hasOverlap = await HasOverlapAsync(model.RoomId, model.StartTime, model.EndTime);
                if (hasOverlap)
                {
                    await transaction.RollbackAsync();
                    return Result<BookingModel>.Failure("Кімната вже заброньована на цей час.", ErrorType.Conflict);
                }

                _context.Bookings.Add(booking);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                await _cacheService.InvalidateAsync(booking.RoomId);

            }
            catch (DbUpdateException ex) when (_context.IsSerializationFailure(ex))
            {
                await transaction.RollbackAsync();
                return Result<BookingModel>.Failure(
                    "Хтось щойно забронював цей час. Спробуйте, будь ласка, інший слот.",
                    ErrorType.Conflict);
            }

            _logger.LogInformation("\r\nBooking {BookingId} for room {RoomId} has been successfully created by user {UserId}.", booking.Id, booking.RoomId, userId);

            return Result<BookingModel>.Success(MapToDto(booking));
        }
        public async Task<Result<bool>> CancelBookingAsync(Guid bookingId, Guid userId, bool isAdmin)
        {
            var result = isAdmin
                    ? await _context.Bookings.FirstOrDefaultAsync(x => x.Id == bookingId)
                    : await _context.Bookings.FirstOrDefaultAsync(x => x.Id == bookingId && x.UserId == userId);

            if (result is null)
            {
                return Result<bool>.Failure($"Бронювання з id {bookingId} не знайдено.", ErrorType.NotFound);
            }

            if (result.Status == BookingStatus.Cancelled)
            {
                return Result<bool>.Failure("Бронювання вже скасоване.", ErrorType.Conflict);
            }

            if (result.StartTime <= DateTime.UtcNow)
            {
                return Result<bool>.Failure("Неможливо скасувати бронювання, яке вже розпочалося або завершилося.", ErrorType.Conflict);
            }

            result.Status = BookingStatus.Cancelled;
            await _context.SaveChangesAsync();

            await _cacheService.InvalidateAsync(result.RoomId);

            _logger.LogInformation("Booking {BookingId} has been successfully cancelled by user {UserId}.", bookingId, userId);
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
                return Result<Room>.Failure("Кімнату не знайдено", ErrorType.NotFound);

            if (!room.IsActive)
                return Result<Room>.Failure("Ця кімната наразі недоступна для бронювання.", ErrorType.Conflict);

            if (attendeesCount > room.Capacity)
                return Result<Room>.Failure($"Забагато людей для цієї кімнати, її ємність {room.Capacity} людей", ErrorType.Validation);

            return Result<Room>.Success(room);
        }
        private static string? BookingTimeValidation(CreateBookingModel model)
        {
            if (model.StartTime < DateTime.UtcNow)
                return "Неможливо створити бронювання в минулому.";

            if (model.StartTime >= model.EndTime)
                return "Час завершення має бути пізніше за час початку.";     

            if (model.EndTime - model.StartTime > MaxBookingDuration)
                return $"Максимальна тривалість бронювання — {MaxBookingDuration.TotalDays:0} днів.";

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
