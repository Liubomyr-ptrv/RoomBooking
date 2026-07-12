using System.Security.Principal;

namespace RoomBooking.Domain.Entities
{
    public class User 
    {
        public Guid Id { get; set; }

        public string Email { get; set; }
        public string FirstName { get; set; }
        public string SecondName { get; set; }
        public string LastName { get; set; }
       
        public string PhoneNumber { get; set; }
        public DateTime CreatedAt { get; set; }

        public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
    }
}
