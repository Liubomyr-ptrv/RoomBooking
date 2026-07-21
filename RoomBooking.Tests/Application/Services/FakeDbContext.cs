using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using RoomBooking.Application.Abstractions.Db;
using RoomBooking.Domain.Entities;
using System.Data;

namespace RoomBooking.Tests.Application.Services
{
    public class FakeAppDbContext : DbContext, IAppDbContext
    {
        private DbSet<User> _users;
        private DbSet<Room> _rooms;
        private DbSet<Booking> _bookings;

        public FakeAppDbContext(DbContextOptions<FakeAppDbContext> options) : base(options) { }

        public DbSet<User> Users { get => Set<User>(); set => _users = value; }
        public DbSet<Room> Rooms { get => Set<Room>(); set => _rooms = value; }
        public DbSet<Booking> Bookings { get => Set<Booking>(); set => _bookings = value; }

        public Task<IDbContextTransaction> BeginTransactionAsync(
            IsolationLevel isolationLevel,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IDbContextTransaction>(new NoOpTransaction());
        }

        public bool IsSerializationFailure(Exception ex) => false;

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return base.SaveChangesAsync(cancellationToken);
        }

        private class NoOpTransaction : IDbContextTransaction
        {
            public Guid TransactionId => Guid.NewGuid();
            public Task CommitAsync(CancellationToken ct = default) => Task.CompletedTask;
            public Task RollbackAsync(CancellationToken ct = default) => Task.CompletedTask;
            public void Commit() { }
            public void Rollback() { }
            public void Dispose() { }
            public ValueTask DisposeAsync() => ValueTask.CompletedTask;
        }
    }
    public static class FakeAppDbContextFactory
    {
        public static FakeAppDbContext Create()
        {
          
            var options = new DbContextOptionsBuilder<FakeAppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            var context = new FakeAppDbContext(options);

            context.Database.EnsureCreated();

            return context;
        }
    }
}
