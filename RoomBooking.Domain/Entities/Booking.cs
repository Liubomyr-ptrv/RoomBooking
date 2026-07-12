using RoomBooking.Domain.Enums;

namespace RoomBooking.Domain.Entities
{
    public class Booking
    {
        public Guid Id { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string Title { get; set; }
        public int AttendeesCount { get; set; }
        public decimal TotalPrice { get; set; }
        public string Notes { get; set; }
        public BookingStatus Status { get; set; }
        
        public Guid UserId { get; set; }
        public User User { get; set; }
        public Guid RoomId { get; set; }
        public Room Room { get; set; }
    }
}
