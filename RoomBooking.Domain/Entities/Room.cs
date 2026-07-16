namespace RoomBooking.Domain.Entities
{
    public class Room
    {    
        public Guid Id { get; set; }
        public required string Name { get; set; }
        public string? Description { get; set; }
        public required string Location { get; set; }
        public required int Capacity { get; set; }
        public string[] Equipment { get; set; }
        public required decimal PricePerHour { get; set; }
        public bool IsActive { get; set; } = true;

        public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
    }
}
