using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using RoomBooking.Application.Abstractions.Db;
using RoomBooking.Application.Abstractions.Services;
using RoomBooking.Application.Services;
using RoomBooking.Application.Settings;
using RoomBooking.Infrastructure.Db;
using RoomBooking.Infrastructure.Extensions;
using RoomBooking.Infrastructure.Services.Caching;
using StackExchange.Redis;


var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Description = "Input JWT in format: Bearer {token}",
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        { new OpenApiSecuritySchemeReference("Bearer", document), [] }
    });
});

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IAppDbContext>(provider => provider.GetRequiredService<AppDbContext>());

builder.Services.AddIdentityServices();

builder.Services.AddOptions<JwtConfigurationOptions>()
    .BindConfiguration("JwtSettings");

builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddAuthorization();


var redisConnectionString = builder.Configuration.GetConnectionString("Redis")!;

builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = $"{redisConnectionString},abortConnect=false";
});

builder.Services.AddScoped<IAvailabilityCacheService, RedisAvailabilityCacheService>();

builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IRoleService, RoleService>();
builder.Services.AddScoped<IRoomService, RoomService>();
builder.Services.AddScoped<IBookingService, BookingService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
