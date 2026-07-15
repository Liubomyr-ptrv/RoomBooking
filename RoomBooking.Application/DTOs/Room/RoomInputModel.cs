using System.ComponentModel.DataAnnotations;

namespace RoomBooking.Application.DTOs.Room
{
    public class RoomInputModel
    {
        [Required(ErrorMessage = "Назва кімнати обов'язкова.")]
        [MaxLength(200)]
        public required string Name { get; set; }

        public string? Description { get; set; }

        [Required]
        public required string Location { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Місткість має бути більшою за 0.")]
        public int Capacity { get; set; }

        public string[] Equipment { get; set; } 

        [Range(0, double.MaxValue, ErrorMessage = "Ціна не може бути від'ємною.")]
        public decimal PricePerHour { get; set; }
    }
}
