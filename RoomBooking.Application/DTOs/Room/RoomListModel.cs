namespace RoomBooking.Application.DTOs.Room
{
    public class RoomListModel
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; } 
        public string Location { get; set; } 
        public int Capacity { get; set; }
        public string[] Equipment { get; set; } 
        public decimal PricePerHour { get; set; }
        public bool IsActive { get; set; }
    }
}
