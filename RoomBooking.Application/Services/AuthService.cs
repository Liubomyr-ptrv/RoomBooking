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
         
            var token = GenerateJwtToken(existing);

            return Result<string>.Success(token);
        }

        public async Task<Result<string>> Register(RegisterModel dto)
        {
            var existing = await _userManager.FindByEmailAsync(dto.Email);
            if (existing is not null)
                return Result<string>.Failure($"Користувач з email '{dto.Email}' вже існує.",ExeptionType.Conflict);
            
            var user = new User
            {
                Id = Guid.NewGuid(),
                UserName = dto.Email,
                Email = dto.Email,
                PhoneNumber = dto.PhoneNumber,
                FirstName = dto.FirstName,
                SecondName = dto.SecondName,
                LastName = dto.LastName,
                CreatedAt = DateTime.UtcNow
            };

            var identityResult = await _userManager.CreateAsync(user, dto.Password);
            if (!identityResult.Succeeded)
                return IdentityErrors(identityResult.Errors);

            var token = GenerateJwtToken(user);

            return Result<string>.Success(token);
        }
        private Result<string> IdentityErrors(IEnumerable<IdentityError> errors)
        {
            var errorList = errors.ToList();
            if(errorList.Any(c => c.Code.Contains("Password")))
            {
                return Result<string>.Failure("Пароль не відповідає вимогам безпеки: мінімум 8 символів.", ExeptionType.Validation);
            }
            if (errorList.Any(c => c.Code.Contains("Email")))
            {
                return Result<string>.Failure("Некоректний формат email.", ExeptionType.Validation);
            }

            return Result<string>.Failure(
               string.Join("; ", errorList.Select(e => e.Description)),
               ExeptionType.IdentityError);
        }
        private string GenerateJwtToken(User user)
        {
            
            var secretKey = _jwtConfigOptions.Value.Key;

            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email!),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

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
