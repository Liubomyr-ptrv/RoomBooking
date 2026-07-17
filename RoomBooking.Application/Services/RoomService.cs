using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using RoomBooking.Application.Abstractions.Db;
using RoomBooking.Application.Abstractions.Services;
using RoomBooking.Application.Common;
using RoomBooking.Application.DTOs.Room;
using RoomBooking.Domain.Entities;
using RoomBooking.Domain.Enums;
using StackExchange.Redis;
using System.Text.Json;

namespace RoomBooking.Application.Services
{
    public class RoomService : IRoomService
    {
        private readonly IAppDbContext _context;
        private readonly IDistributedCache _cache;
        private readonly IConnectionMultiplexer _redis;
        private static readonly int MaxAvailabilityRangeDays = 31;

        public RoomService(IAppDbContext context, IDistributedCache cache, IConnectionMultiplexer redis)
        {
            _context = context;
            _cache = cache;
            _redis = redis;
        }
        public async Task<Result<RoomModel>> GetByIdAsync(Guid id)
        {
            var result = await _context.Rooms.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
            if (result is null)
            {
                return Result<RoomModel>.Failure($"Кімнату з id {id} не знайдено.", ErrorType.NotFound);
            }
            var room = MapToDto(result);

            return Result<RoomModel>.Success(room);
        }

        public async Task<Result<List<RoomListModel>>> GetAllAsync()
        {
            var result = await _context.Rooms
                .AsNoTracking()
                .Where(x => x.IsActive)   
                .ToListAsync();

            var rooms = Result<List<RoomListModel>>.Success(result.Select(MapToListDto).ToList());

            return rooms;
        }
        public async Task<Result<List<TimeSlotModel>>> GetAvailabilityAsync(Guid roomId, DateTime dateFrom, DateTime dateTo)
        {
            var timeValidation = TimeValidation(dateFrom, dateTo);
            if (timeValidation is not null)
            {
                return Result<List<TimeSlotModel>>.Failure(timeValidation, ErrorType.Validation);
            }

            var cacheKey = $"room-availability:{roomId}:{dateFrom:yyyy-MM-ddTHH-mm}:{dateTo:yyyy-MM-ddTHH-mm}";

            var cached = await _cache.GetStringAsync(cacheKey);
            if (cached is not null)
            {
                try
                {
                    var cachedSlots = JsonSerializer.Deserialize<List<TimeSlotModel>>(cached);
                    if (cachedSlots is not null)
                    {
                        return Result<List<TimeSlotModel>>.Success(cachedSlots);
                    }
                }
                catch (JsonException)
                {
                   
                }
            }

            var room = await _context.Rooms.AsNoTracking().FirstOrDefaultAsync(x => x.Id == roomId);
            if (room is null)
            {
                return Result<List<TimeSlotModel>>.Failure("Кімнату не знайдено.", ErrorType.NotFound);
            }

            var bookings = await _context.Bookings
                .AsNoTracking()
                .Where(x => x.RoomId == roomId
                          && x.Status != BookingStatus.Cancelled
                          && x.StartTime < dateTo
                          && x.EndTime > dateFrom)
                .OrderBy(b => b.StartTime)
                .Select(b => new TimeSlotModel
                {
                    StartTime = b.StartTime,
                    EndTime = b.EndTime,
                    IsBooked = true
                })
                .ToListAsync();

            await _cache.SetStringAsync(
                    cacheKey,
                    JsonSerializer.Serialize(bookings),
                    new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5) });

            return Result<List<TimeSlotModel>>.Success(bookings);
        }
        public async Task<Result<RoomModel>> CreateAsync(RoomInputModel model)
        {
            var validationError = ValidateRoomInput(model);
            if (validationError is not null)
                return Result<RoomModel>.Failure(validationError, ErrorType.Validation);

            var nameExists = await _context.Rooms
                .AnyAsync(r => r.Name.ToLower() == model.Name.ToLower());

            if (nameExists)
                return Result<RoomModel>.Failure( $"Кімната з назвою '{model.Name}' вже існує.", ErrorType.Conflict);

            var room = new Room
            {
                Id = Guid.NewGuid(),
                Name = model.Name,
                Description = model.Description ?? string.Empty,
                Location = model.Location,
                Capacity = model.Capacity,
                Equipment = model.Equipment,
                PricePerHour = model.PricePerHour,
                IsActive = true
            };

             _context.Rooms.Add(room);
            await _context.SaveChangesAsync();

            return Result<RoomModel>.Success(MapToDto(room));
        }

        public async Task<Result<RoomModel>> UpdateAsync(Guid id, RoomInputModel model)
        {
            var validationError = ValidateRoomInput(model);
            if (validationError is not null)
                return Result<RoomModel>.Failure(validationError, ErrorType.Validation);

            var updateRoom = await _context.Rooms.FirstOrDefaultAsync(r => r.Id == id);
            if (updateRoom is null)
                return Result<RoomModel>.Failure($"Кімната з id {id} не існує",ErrorType.NotFound);

            var nameTaken = await _context.Rooms
                .AnyAsync(r => r.Id != id && r.Name.ToLower() == model.Name.ToLower());

            if (nameTaken)
                return Result<RoomModel>.Failure( $"Кімната з назвою '{model.Name}' вже існує.",  ErrorType.Conflict);

            updateRoom.Name = model.Name;
            updateRoom.Description = model.Description ?? string.Empty;
            updateRoom.Location = model.Location;
            updateRoom.Capacity = model.Capacity;
            updateRoom.Equipment = model.Equipment;
            updateRoom.PricePerHour = model.PricePerHour;
            
            await _context.SaveChangesAsync();

            return Result<RoomModel>.Success(MapToDto(updateRoom));
        }

        public async Task<Result<bool>> DeleteAsync(Guid id)
        {
            var deletedRoom = await _context.Rooms.FirstOrDefaultAsync(r => r.Id == id);
            if (deletedRoom is null)
                return Result<bool>.Failure("Кімнату не знайдено", ErrorType.NotFound);

            if (!deletedRoom.IsActive)
                return Result<bool>.Failure("Кімната вже деактивована.", ErrorType.Conflict);

            var hasActiveBookings = await _context.Bookings.AnyAsync(b =>
                 b.RoomId == id &&
                 b.Status != BookingStatus.Cancelled &&
                 b.EndTime > DateTime.UtcNow);

            if (hasActiveBookings)
                return Result<bool>.Failure(
                    "Неможливо деактивувати кімнату, поки на неї є активні бронювання.",
                    ErrorType.Conflict);
                    

            deletedRoom.IsActive = false;

             await _context.SaveChangesAsync();
               await InvalidateAvailabilityCacheAsync(id);

            return Result<bool>.Success(true);
        }
        public async Task InvalidateAvailabilityCacheAsync(Guid roomId)
        {
            try
            {
                var pattern = $"room-availability:{roomId}:*";
                var db = _redis.GetDatabase();

                foreach (var endpoint in _redis.GetEndPoints())
                {
                    var server = _redis.GetServer(endpoint);
                    if (server.IsReplica) continue; 

                    await foreach (var key in server.KeysAsync(pattern: pattern))
                    {           
                        await db.KeyDeleteAsync(key);
                    }
                }
            }
            catch (Exception)
            {
             
            }
        }
        private static string? TimeValidation(DateTime dateFrom, DateTime dateTo)
        {
            if (dateFrom == default)
                return "Дата початку є обов'язковою.";

            if (dateTo == default)
                return "Дата завершення є обов'язковою.";

            if (dateFrom < DateTime.UtcNow.Date)
                return "Дата початку не може бути в минулому.";

            if (dateFrom >= dateTo)
                return "Дата початку має бути раніше дати завершення.";

            if ((dateTo - dateFrom).TotalDays > MaxAvailabilityRangeDays)
                return $"Максимальний період перегляду доступності — {MaxAvailabilityRangeDays} днів.";

            return null;
        }
        private static string? ValidateRoomInput(RoomInputModel model)
        {
            if (string.IsNullOrWhiteSpace(model.Name))
                return "Назва кімнати не може бути порожньою.";

            if (model.Capacity <= 0)
                return "Місткість кімнати має бути більшою за 0.";

            if (model.PricePerHour < 0)
                return "Ціна за годину не може бути від'ємною.";

            return null;
        }

        private static RoomModel MapToDto(Room room)
        {
            return new RoomModel
            {
                Id = room.Id,
                Name = room.Name,
                Description = room.Description,
                Location = room.Location,
                Capacity = room.Capacity,
                Equipment = room.Equipment,
                PricePerHour = room.PricePerHour,    
            };
        }
        private static RoomListModel MapToListDto(Room room)
        {
            return new RoomListModel
            {
                Id = room.Id,
                Name = room.Name,
                Description = room.Description,
                Location = room.Location,
                Capacity = room.Capacity,
                Equipment = room.Equipment,
                PricePerHour = room.PricePerHour,
                IsActive = room.IsActive
            };
        }
    }
}
