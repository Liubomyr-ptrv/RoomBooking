using Microsoft.EntityFrameworkCore;
using RoomBooking.Application.Abstractions.Db;
using RoomBooking.Application.Abstractions.Services;
using RoomBooking.Application.Common;
using RoomBooking.Application.DTOs.Room;
using RoomBooking.Domain.Entities;
using RoomBooking.Domain.Enums;

namespace RoomBooking.Application.Services
{
    public class RoomService : IRoomService
    {
        private readonly IAppDbContext _context;
        public RoomService(IAppDbContext context)
        {
            _context = context;
        }
        public async Task<Result<RoomModel>> GetByIdAsync(Guid id)
        {
            var result = await _context.Rooms.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
            if (result is null)
            {
                return Result<RoomModel>.Failure($"Кімнату з id {id} не знайдено.", ExeptionType.NotFound);
            }
            var room = MapToDto(result);

            return Result<RoomModel>.Success(room);
        }

        public async Task<Result<List<RoomModel>>> GetAllAsync()
        {
            var result = await _context.Rooms.AsNoTracking().ToListAsync();

            var rooms = Result<List<RoomModel>>.Success(result.Select(MapToDto).ToList());

            return rooms;
        }

        public async Task<Result<RoomModel>> CreateAsync(RoomInputModel model)
        {
            var nameExists = await _context.Rooms
                .AnyAsync(r => r.Name.ToLower() == model.Name.ToLower());

            if (nameExists)
                return Result<RoomModel>.Failure( $"Кімната з назвою '{model.Name}' вже існує.", ExeptionType.Conflict);

            var room = new Room
            {
                Id = Guid.NewGuid(),
                Name = model.Name,
                Description = model.Description ?? string.Empty,
                Location = model.Location,
                Capacity = model.Capacity,
                Equipment = model.Equipment,
                PricePerHour = model.PricePerHour,
                IsBooked = false,
                IsActive = true
            };

            var result = _context.Rooms.Add(room);
            await _context.SaveChangesAsync();

            return Result<RoomModel>.Success(MapToDto(room));
        }

        public async Task<Result<RoomModel>> UpdateAsync(Guid id, RoomInputModel model)
        {
            var updateRoom = await _context.Rooms.FirstOrDefaultAsync(r => r.Id == id);
            if (updateRoom is null)
                return Result<RoomModel>.Failure($"Кімната з id {id} не існує",ExeptionType.NotFound);

            var nameTaken = await _context.Rooms
                .AnyAsync(r => r.Id != id && r.Name.ToLower() == model.Name.ToLower());

            if (nameTaken)
                return Result<RoomModel>.Failure( $"Кімната з назвою '{model.Name}' вже існує.",  ExeptionType.Conflict);

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
                return Result<bool>.Failure("Кімнату не знайдено", ExeptionType.NotFound);

            if (!deletedRoom.IsActive)
                return Result<bool>.Failure("Кімната вже деактивована.", ExeptionType.Conflict);

            deletedRoom.IsActive = false;

            var savedRows = await _context.SaveChangesAsync();

            return Result<bool>.Success(true);
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
                IsBooked = room.IsBooked,
                IsActive = room.IsActive
            };
        }
    }
}
