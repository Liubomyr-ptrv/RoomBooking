using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using RoomBooking.Domain.Entities;
using System.Data;

namespace RoomBooking.Application.Abstractions.Db
{
    public interface IAppDbContext
    {
        public DbSet<User> Users { get; set; }
        public DbSet<Room> Rooms { get; set; }
        public DbSet<Booking> Bookings { get; set; }
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
        public bool IsSerializationFailure(Exception ex);

        Task<IDbContextTransaction> BeginTransactionAsync(
           IsolationLevel isolationLevel,
           CancellationToken cancellationToken = default);

    }
}
