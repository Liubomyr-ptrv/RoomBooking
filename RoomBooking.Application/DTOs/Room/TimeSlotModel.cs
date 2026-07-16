namespace RoomBooking.Application.DTOs.Room
{
    public class TimeSlotModel
    {
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public bool IsBooked { get; set; }
    }
}
