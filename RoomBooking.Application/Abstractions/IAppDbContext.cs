using Microsoft.EntityFrameworkCore;
using RoomBooking.Domain.Entities;

namespace RoomBooking.Application.Abstractions
{
    public interface IAppDbContext
    {
        public DbSet<User> Users { get; set; }
        public DbSet<Room> Rooms { get; set; }
        public DbSet<Booking> Bookings { get; set; }
    }
}
