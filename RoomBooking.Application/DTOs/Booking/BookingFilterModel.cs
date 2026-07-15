using RoomBooking.Domain.Enums;

namespace RoomBooking.Application.DTOs.Booking
{
    public class BookingFilterModel
    {
        public Guid? RoomId { get; set; }
        public Guid? UserId { get; set; }
        public BookingStatus? Status { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
    }
}
