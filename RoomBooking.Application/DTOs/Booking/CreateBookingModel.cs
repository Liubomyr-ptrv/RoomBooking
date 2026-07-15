using System.ComponentModel.DataAnnotations;

namespace RoomBooking.Application.DTOs.Booking
{
    public class CreateBookingModel
    {
        [Required(ErrorMessage = "Час початку бронювання обов'язковий.")]
        public required DateTime StartTime { get; set; }

        [Required(ErrorMessage = "Час завершення бронювання обов'язковий.")]
        public required DateTime EndTime { get; set; }

        [MaxLength(200, ErrorMessage = "Назва не може перевищувати 200 символів.")]
        public string? Title { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Кількість учасників має бути більшою за 0.")]
        public int AttendeesCount { get; set; }

        [MaxLength(1000, ErrorMessage = "Примітка не може перевищувати 1000 символів.")]
        public string? Notes { get; set; }

        [Required(ErrorMessage = "Кімната обов'язкова.")]
        public Guid RoomId { get; set; }
    }
}
