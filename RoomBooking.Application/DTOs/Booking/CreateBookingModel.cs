namespace RoomBooking.Application.DTOs.Booking
{
    public class CreateBookingModel
    {  
        public required DateTime StartTime { get; set; }
        public required DateTime EndTime { get; set; }
        public  string? Title { get; set; }
        public int AttendeesCount { get; set; }
        public string? Notes { get; set; }
        public Guid RoomId { get; set; }
    }
}
