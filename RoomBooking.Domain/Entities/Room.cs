namespace RoomBooking.Domain.Entities
{
    public class Room
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Location { get; set; }
        public int Capicity { get; set; }
        public string[] Equipment { get; set; }
        public decimal PricePerHouse { get; set; }
        public bool IsBooked { get; set; }

        public ICollection<Booking> Bookings { get; set; }
    }
}
