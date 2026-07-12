using Microsoft.EntityFrameworkCore;
using RoomBooking.Application.Abstractions;
using RoomBooking.Domain.Entities;

namespace RoomBooking.Infrastructure
{
    public class AppDbContext : DbContext, IAppDbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
        public DbSet<User> Users { get; set; }
        public DbSet<Room> Rooms { get; set; }
        public DbSet<Booking> Bookings { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Booking>()
                .HasOne(c => c.User)
                .WithMany(c => c.Bookings)
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Booking>()
               .HasOne(c => c.Room)
               .WithMany(c => c.Bookings)
               .HasForeignKey(c => c.RoomId)
               .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
