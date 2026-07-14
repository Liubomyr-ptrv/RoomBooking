using RoomBooking.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace RoomBooking.Application.DTOs.Auth
{
    public class AssignRoleModel
    {
        [Required(ErrorMessage = "Ідентифікатор користувача обов'язковий.")]
        public required Guid UserId { get; set; }

        [Required(ErrorMessage = "Роль обов'язкова.")]
        public required UserRole Role { get; set; }
    }
}
