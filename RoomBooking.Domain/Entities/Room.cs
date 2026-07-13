using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RoomBooking.Domain.Entities
{
    public class Room
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Location { get; set; }
        public int Capacity { get; set; }
        public string[] Equipment { get; set; }
        public decimal PricePerHouse { get; set; }
        public bool IsBooked { get; set; }
        public bool IsActive { get; set; } = true;

        public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
    }
}
