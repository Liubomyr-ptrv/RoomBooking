using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using RoomBooking.Application.Abstractions.Services;
using RoomBooking.Application.DTOs.Auth;
using RoomBooking.Application.Settings;
using RoomBooking.Domain.Entities;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace RoomBooking.Infrastructure.Services
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
        public async Task<string> Login(LoginDto dto)
        {
            var existing = await _userManager.FindByEmailAsync(dto.Email);
            if (existing is null)
                throw new InvalidOperationException("Невірний email або пароль.");

            var signInResult = await _signInManager.CheckPasswordSignInAsync(existing, dto.Password, lockoutOnFailure: true);

            if (signInResult.IsLockedOut)
                throw new InvalidOperationException("Акаунт тимчасово заблоковано через забагато невдалих спроб.");

            if (!signInResult.Succeeded)
                throw new InvalidOperationException("Невірний email або пароль.");

            var token = GenerateJwtToken(existing);

            return token;
        }

        public async Task<string> Register(RegisterDto dto)
        {
            var existing = await _userManager.FindByEmailAsync(dto.Email);
            if (existing is not null)
                throw new InvalidOperationException($"Користувач з email '{dto.Email}' вже існує");

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
                throw new Exception(string.Join("; ", identityResult.Errors.Select(e => e.Description)));

            return GenerateJwtToken(user);
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
