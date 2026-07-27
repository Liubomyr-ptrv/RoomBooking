using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RoomBooking.Domain.Enums;
using RoomBooking.Infrastructure.Db;

namespace RoomBooking.Infrastructure.Services.BackgroundServices
{
    public class BookingReminderService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<BookingReminderService> _logger;
        private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(5);
        private readonly TimeSpan _reminderWindow = TimeSpan.FromMinutes(15);

        public BookingReminderService(IServiceScopeFactory scopeFactory, ILogger<BookingReminderService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CheckAndSendRemindersAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    
                    _logger.LogError(ex, "Error while checking booking reminders");
                }

                await Task.Delay(_checkInterval, stoppingToken);
            }
        }
        private async Task CheckAndSendRemindersAsync(CancellationToken stoppingToken)
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var now = DateTime.UtcNow;
            var threshold = now.AddMinutes(_reminderWindow.TotalMinutes);

            var upcoming = await context.Bookings
                .Where(b => b.Status == BookingStatus.Confirmed     
                         && b.StartTime > now
                         && b.StartTime <= threshold)
                .ToListAsync(stoppingToken);

            if (upcoming.Count == 0)
            {
                _logger.LogDebug("No upcoming bookings requiring reminders");
                return;
            }

            foreach (var booking in upcoming)
            {
                _logger.LogInformation(
                    "Reminder: booking {BookingId} for user {UserId} starts at {StartTime}",
                    booking.Id, booking.UserId, booking.StartTime);
            }

            await context.SaveChangesAsync(stoppingToken);
        }

    }
}
