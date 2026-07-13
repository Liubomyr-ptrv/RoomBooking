using RoomBooking.Domain.Enums;

namespace RoomBooking.Application.DTOs.Auth
{
    public class AssignRoleModel
    {
        public Guid UserId { get; set; }
        public UserRole Role { get; set; }
    }
}
