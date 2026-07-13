using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using RoomBooking.Application.Abstractions.Services;
using RoomBooking.Application.Common;
using RoomBooking.Application.DTOs.Auth;
using RoomBooking.Application.Settings;
using RoomBooking.Domain.Entities;
using RoomBooking.Domain.Enums;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace RoomBooking.Application.Services
{
    public class AuthService : IAuthService
    {
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;
        private readonly IOptions<JwtConfigurationOptions> _jwtConfigOptions;
        public AuthService(
            UserManager<User> userManager,
            SignInManager<User> signInManager,
            IOptions<JwtConfigurationOptions> jwtConfigOptions)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _jwtConfigOptions = jwtConfigOptions;
        }
        public async Task<Result<string>> Login(LoginModel dto)
        {
            var existing = await _userManager.FindByEmailAsync(dto.Email);
            if (existing is null)
                return Result<string>.Failure($"Користувача з email '{dto.Email}' не знайдено.", ExeptionType.NotFound);

            var signInResult = await _signInManager.CheckPasswordSignInAsync(existing, dto.Password, lockoutOnFailure: true);

            if (signInResult.IsLockedOut)
                return Result<string>.Failure("Акаунт тимчасово заблоковано через забагато невдалих спроб.", ExeptionType.Forbidden);


            if (!signInResult.Succeeded)
                return Result<string>.Failure("Невірний email або пароль.", ExeptionType.Validation);

            var token = await GenerateJwtToken(existing);

            return Result<string>.Success(token);
        }

        public async Task<Result<string>> Register(RegisterModel model)
        {
            var existing = await _userManager.FindByEmailAsync(model.Email);
            if (existing is not null)
                return Result<string>.Failure($"Користувач з email '{model.Email}' вже існує.", ExeptionType.Conflict);

            var user = new User
            {
                Id = Guid.NewGuid(),
                UserName = model.Email,
                Email = model.Email,
                PhoneNumber = model.PhoneNumber,
                FirstName = model.FirstName,
                SecondName = model.SecondName,
                LastName = model.LastName,
                CreatedAt = DateTime.UtcNow
            };

            var identityResult = await _userManager.CreateAsync(user, model.Password);
            if (!identityResult.Succeeded)
                return IdentityErrors(identityResult.Errors);

            var roleResult = await _userManager.AddToRoleAsync(user, nameof(UserRole.Client));
            if (!roleResult.Succeeded)
            {
                return Result<string>.Failure("Не вдалось призначити роль користувачу.",ExeptionType.InternalServerError);
            }

            var token = await GenerateJwtToken(user);

            return Result<string>.Success(token);
        }
        public async Task<Result<bool>> AssignRole(AssignRoleModel model)
        {
            if (!Enum.IsDefined(typeof(UserRole), model.Role))
                return Result<bool>.Failure("Невалідна роль.", ExeptionType.Validation);

            var user = await _userManager.FindByIdAsync(model.UserId.ToString());
            if(user is null)
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

        public async Task<Result<bool>> RemoveRole(AssignRoleModel model)
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
        private Result<string> IdentityErrors(IEnumerable<IdentityError> errors)
        {
            var errorList = errors.ToList();
            if (errorList.Any(c => c.Code.Contains("Password")))
            {
                return Result<string>.Failure("Пароль не відповідає вимогам безпеки: мінімум 8 символів.", ExeptionType.Validation);
            }
            if (errorList.Any(c => c.Code.Contains("Email")))
            {
                return Result<string>.Failure("Некоректний формат email.", ExeptionType.Validation);
            }

            return Result<string>.Failure(
               string.Join("; ", errorList.Select(e => e.Description)),
               ExeptionType.InternalServerError);
        }
        private async Task<string> GenerateJwtToken(User user)
        {

            var secretKey = _jwtConfigOptions.Value.Key;

            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var roles = await _userManager.GetRolesAsync(user);

            var claims = new List<Claim>
            {
            new (JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new (JwtRegisteredClaimNames.Email, user.Email!),
            new (JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
              };

            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddHours(_jwtConfigOptions.Value.ExpirationInHours),
                Issuer = _jwtConfigOptions.Value.Issuer,
                Audience = _jwtConfigOptions.Value.Audience,
                SigningCredentials = credentials
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);

            return tokenHandler.WriteToken(token);
        }    
    }
}
