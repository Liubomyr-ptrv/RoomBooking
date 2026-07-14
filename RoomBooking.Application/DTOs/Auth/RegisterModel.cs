using System.ComponentModel.DataAnnotations;

namespace RoomBooking.Application.DTOs.Auth
{
    public class RegisterModel
    {
        [Required(ErrorMessage = "Email обов'язковий.")]
        [EmailAddress]
        public required string Email { get; set; }

        [Required(ErrorMessage = "Пароль обов'язковий.")]
        public required string Password { get; set; }

        [Required(ErrorMessage = "Ім'я обов'язкове.")]
        public required string FirstName { get; set; }

        [Required(ErrorMessage = "По батькові обов'язкове.")]
        public required string SecondName { get; set; }

        [Required(ErrorMessage = "Прізвище обов'язкове.")]
        public required string LastName { get; set; }

        [Required(ErrorMessage = "Номер телефону обов'язковий.")]
        [Phone]
        public required string PhoneNumber { get; set; }
    }
}
