using Microsoft.AspNetCore.Identity;
using RoomBooking.Application.Abstractions.Services;
using RoomBooking.Application.Common;
using RoomBooking.Application.DTOs.Roles;
using RoomBooking.Domain.Entities;
using RoomBooking.Domain.Enums;

namespace RoomBooking.Application.Services
{
    public class RoleService : IRoleService
    {
        private readonly UserManager<User> _userManager;
        public RoleService(UserManager<User> userManager)
        {
            _userManager = userManager;
        }
        public async Task<Result<bool>> AssignRoleAsync(AssignRoleModel model)
        {
            if (!Enum.IsDefined(typeof(UserRole), model.Role))
                return Result<bool>.Failure("Невалідна роль.", ExeptionType.Validation);

            var user = await _userManager.FindByIdAsync(model.UserId.ToString());
            if (user is null)
            {
                return Result<bool>.Failure($"Користувач з id {model.UserId} не існує", ExeptionType.NotFound);
            }

            var roleName = model.Role.ToString();

            var alreadyInRole = await _userManager.IsInRoleAsync(user, roleName);
            if (alreadyInRole)
                return Result<bool>.Failure("Користувач вже має цю роль.", ExeptionType.Conflict);

            var roleResult = await _userManager.AddToRoleAsync(user, roleName);
            if (!roleResult.Succeeded)
            {
                return Result<bool>.Failure("Не вдалось призначити роль користувачу.", ExeptionType.InternalServerError);
            }

            if (model.Role == UserRole.Admin && !await _userManager.IsInRoleAsync(user, nameof(UserRole.Client)))
            {
                await _userManager.AddToRoleAsync(user, nameof(UserRole.Client));
            }

            return Result<bool>.Success(true);
        }

        public async Task<Result<bool>> RemoveRoleAsync(AssignRoleModel model)
        {
            if (!Enum.IsDefined(typeof(UserRole), model.Role))
                return Result<bool>.Failure("Невалідна роль.", ExeptionType.Validation);

            var user = await _userManager.FindByIdAsync(model.UserId.ToString());
            if (user is null)
            {
                return Result<bool>.Failure($"Користувач з id {model.UserId} не існує", ExeptionType.NotFound);
            }
            var roleName = model.Role.ToString();

            if (roleName == nameof(UserRole.Admin))
            {
                var admins = await _userManager.GetUsersInRoleAsync(nameof(UserRole.Admin));
                if (admins.Count <= 1)
                    return Result<bool>.Failure("Неможливо видалити останнього адміністратора.", ExeptionType.Forbidden);
            }

            var roleResult = await _userManager.RemoveFromRoleAsync(user, roleName);
            if (!roleResult.Succeeded)
            {
                return Result<bool>.Failure("Не вдалось видалити роль користувачу.", ExeptionType.InternalServerError);
            }

            return Result<bool>.Success(true);
        }
    }
}
