namespace RoomBooking.Application.DTOs.Room
{
    public class RoomInputModel
    {   
        public string Name { get; set; }
        public string? Description { get; set; }
        public string Location { get; set; }
        public int Capacity { get; set; }
        public string[] Equipment { get; set; }
        public decimal PricePerHour { get; set; }  
    }
}
