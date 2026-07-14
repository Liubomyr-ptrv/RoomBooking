using RoomBooking.Domain.Enums;

namespace RoomBooking.Application.DTOs.Booking
{
    public class BookingModel
    {
        public Guid Id { get; set; }
        public Guid RoomId { get; set; }
        public required DateTime StartTime { get; set; }
        public required DateTime EndTime { get; set; }
        public  string? Title { get; set; }
        public int AttendeesCount { get; set; }
        public required decimal TotalPrice { get; set; }
        public string? Notes { get; set; }
        public BookingStatus Status { get; set; }
    }
}
