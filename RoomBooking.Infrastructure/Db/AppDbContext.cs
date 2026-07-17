using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;
using RoomBooking.Application.Abstractions.Db;
using RoomBooking.Domain.Entities;
using System.Data;

namespace RoomBooking.Infrastructure.Db
{
    public class AppDbContext : IdentityDbContext<User, IdentityRole<Guid>, Guid>, IAppDbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
        public DbSet<Room> Rooms { get; set; }
        public DbSet<Booking> Bookings { get; set; }

        public bool IsSerializationFailure(Exception ex)
        {
            return ex.InnerException is PostgresException pgEx &&
                   (pgEx.SqlState == "40001" || pgEx.SqlState == "40P01");
        }

        public Task<IDbContextTransaction> BeginTransactionAsync(
                IsolationLevel isolationLevel,
                CancellationToken cancellationToken = default)
        {
            return Database.BeginTransactionAsync(isolationLevel, cancellationToken);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        { 
            base.OnModelCreating(modelBuilder);

            modelBuilder.Ignore<IdentityPasskeyData>();
            modelBuilder.Ignore<IdentityUserPasskey<Guid>>();
            modelBuilder.Ignore<IdentityUserClaim<Guid>>();
            modelBuilder.Ignore<IdentityRoleClaim<Guid>>();
            modelBuilder.Ignore<IdentityUserLogin<Guid>>();
            modelBuilder.Ignore<IdentityUserToken<Guid>>();

            modelBuilder.Entity<Booking>()
                .HasOne(c => c.User)
                .WithMany(c => c.Bookings)
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Booking>()
               .HasOne(c => c.Room)
               .WithMany(c => c.Bookings)
               .HasForeignKey(c => c.RoomId)
               .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<User>()
                .HasIndex(u => u.PhoneNumber)
                .IsUnique();

            modelBuilder.Entity<Booking>()
                .HasIndex(b => new { b.RoomId, b.StartTime, b.EndTime });

            modelBuilder.Entity<Room>()
                 .HasIndex(r => r.Name)
                 .IsUnique();
        }
    }
}
