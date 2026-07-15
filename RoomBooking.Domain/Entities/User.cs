using Microsoft.AspNetCore.Identity;

namespace RoomBooking.Domain.Entities
{
    public class User : IdentityUser<Guid>
    { 
        public required string FirstName { get; set; }
        public required string SecondName { get; set; }
        public required string LastName { get; set; }
        public override required string? PhoneNumber { get; set; }
        public DateTime CreatedAt { get; set; }

        public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
    }
}
