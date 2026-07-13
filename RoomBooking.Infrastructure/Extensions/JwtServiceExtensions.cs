using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using RoomBooking.Application.Settings;
using System.Text;

namespace RoomBooking.Infrastructure.Extensions
{
    public static class JwtServiceExtensions
    {
        public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
        {
            var jwtOptions = configuration.GetSection("JwtSettings").Get<JwtConfigurationOptions>()
                  ?? throw new InvalidOperationException("Секція JwtSettings не налаштована.");


            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
               {
                   options.TokenValidationParameters = new TokenValidationParameters
                   {
                       ValidateIssuer = true,
                       ValidateAudience = true,
                       ValidateLifetime = true,
                       ValidateIssuerSigningKey = true,
                       ClockSkew = TimeSpan.Zero,
                       ValidIssuer = jwtOptions.Issuer,
                       ValidAudience = jwtOptions.Audience,
                       IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Key))
                   };

                   options.Events = new JwtBearerEvents
                   {
                       OnAuthenticationFailed = context =>
                       {
                           Console.WriteLine($"Authentication failed: {context.Exception.Message}");
                           return Task.CompletedTask;
                       },
                       OnChallenge = context =>
                       {
                           Console.WriteLine($"Token validation failed: {context.AuthenticateFailure?.Message}");
                           return Task.CompletedTask;
                       }
                   };
               });
            return services;
        }
    }
}
