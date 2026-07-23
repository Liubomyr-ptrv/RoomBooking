using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
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
        private readonly ILogger<RoleService> _logger;
        public RoleService(UserManager<User> userManager, ILogger<RoleService> logger)
        {
            _userManager = userManager;
            _logger = logger;
        }
        public async Task<Result<bool>> AssignRoleAsync(AssignRoleModel model )
        {
            if (!Enum.IsDefined(typeof(UserRole), model.Role))
                return Result<bool>.Failure("Невалідна роль.", ErrorType.Validation);

            var user = await _userManager.FindByIdAsync(model.UserId.ToString());
            if (user is null)
            {
                return Result<bool>.Failure($"Користувач з id {model.UserId} не існує", ErrorType.NotFound);
            }

            var roleName = model.Role.ToString();

            var alreadyInRole = await _userManager.IsInRoleAsync(user, roleName);
            if (alreadyInRole)
                return Result<bool>.Failure("Користувач вже має цю роль.", ErrorType.Conflict);

            var roleResult = await _userManager.AddToRoleAsync(user, roleName);
            if (!roleResult.Succeeded)
            {
                return Result<bool>.Failure("Не вдалось призначити роль користувачу.", ErrorType.InternalServerError);
            }

            var hasClient = await _userManager.IsInRoleAsync(user, nameof(UserRole.Client));
            var hasAdmin = model.Role == UserRole.Admin;

            if (hasAdmin  && !hasClient)
            {
                await _userManager.AddToRoleAsync(user, nameof(UserRole.Client));
            }

            _logger.LogInformation("The role {Role} has been successfully assigned to user {UserId}.", roleName, model.UserId);
            return Result<bool>.Success(true);
        }

        public async Task<Result<bool>> RemoveRoleAsync(AssignRoleModel model)
        {
            if (!Enum.IsDefined(typeof(UserRole), model.Role))
                return Result<bool>.Failure("Невалідна роль.", ErrorType.Validation);

            var user = await _userManager.FindByIdAsync(model.UserId.ToString());
            if (user is null)
            {
                return Result<bool>.Failure($"Користувач з id {model.UserId} не існує", ErrorType.NotFound);
            }
            var roleName = model.Role.ToString();

            if (roleName == nameof(UserRole.Admin))
            {
                var admins = await _userManager.GetUsersInRoleAsync(nameof(UserRole.Admin));
                if (admins.Count <= 1)
                    return Result<bool>.Failure("Неможливо видалити останнього адміністратора.", ErrorType.Forbidden);
            }

            var roleResult = await _userManager.RemoveFromRoleAsync(user, roleName);
            if (!roleResult.Succeeded)
            {
                return Result<bool>.Failure("Не вдалось видалити роль користувачу.", ErrorType.InternalServerError);
            }

            _logger.LogInformation("Role {Role} successfully removed from user {UserId}.", roleName, model.UserId);
            return Result<bool>.Success(true);
        }
    }
}
