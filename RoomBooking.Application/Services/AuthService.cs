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
        public async Task<Result<string>> LoginAsync(LoginModel dto)
        {
            var existing = await _userManager.FindByEmailAsync(dto.Email);
            if (existing is null)
                return Result<string>.Failure($"Користувача з email '{dto.Email}' не знайдено.", ExeptionType.NotFound);

            var signInResult = await _signInManager.CheckPasswordSignInAsync(existing, dto.Password, lockoutOnFailure: true);

            if (signInResult.IsLockedOut)
                return Result<string>.Failure("Акаунт тимчасово заблоковано через забагато невдалих спроб.", ExeptionType.Forbidden);


            if (!signInResult.Succeeded)
                return Result<string>.Failure("Невірний email або пароль.", ExeptionType.Validation);

            var token = await GenerateJwtTokenAsync(existing);

            return Result<string>.Success(token);
        }

        public async Task<Result<string>> RegisterAsync(RegisterModel model)
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

            var token = await GenerateJwtTokenAsync(user);

            return Result<string>.Success(token);
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
        private async Task<string> GenerateJwtTokenAsync(User user)
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
