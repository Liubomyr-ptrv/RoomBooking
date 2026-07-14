using System.ComponentModel.DataAnnotations;

namespace RoomBooking.Application.DTOs.Auth
{
    public class LoginModel
    {
        [Required(ErrorMessage = "Email обов'язковий.")]
        [EmailAddress(ErrorMessage = "Некоректний формат email.")]
        public required string Email { get; set; }

        [Required(ErrorMessage = "Пароль обов'язковий.")]
        public required string Password { get; set; }
    }
}
