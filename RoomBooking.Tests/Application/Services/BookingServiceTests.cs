using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using RoomBooking.Application.Abstractions.Services;
using RoomBooking.Application.DTOs.Booking;
using RoomBooking.Application.Services;
using RoomBooking.Domain.Entities;
using RoomBooking.Domain.Enums;
using RoomBooking.Infrastructure.Db;
using System.Reflection;
using Xunit;

namespace RoomBooking.Tests.Application.Services
{
    public class BookingServiceOverlapTests : IAsyncLifetime
    {
        private AppDbContext _context = null!;
        private BookingService _service = null!;

        private readonly Guid _roomId = Guid.NewGuid();
        private readonly Guid _otherRoomId = Guid.NewGuid();

        private static readonly DateTime ExistingStart = new(2026, 8, 1, 10, 0, 0, DateTimeKind.Utc);
        private static readonly DateTime ExistingEnd = new(2026, 8, 1, 11, 0, 0, DateTimeKind.Utc);

        private static readonly DateTime AnchorStart = DateTime.UtcNow.Date.AddDays(2).AddHours(9);

        public async Task InitializeAsync()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _context = new AppDbContext(options);

            var cacheMock = new Mock<IAvailabilityCacheService>();
            var loggerMock = new Mock<ILogger<BookingService>>();


            _service = new BookingService(_context, loggerMock.Object, cacheMock.Object);


            await SeedBookingAsync(_roomId, ExistingStart, ExistingEnd, BookingStatus.Confirmed);
        }
        public Task DisposeAsync()
        {
            _context.Dispose();
            return Task.CompletedTask;
        }
        private async Task SeedBookingAsync(Guid roomId, DateTime start, DateTime end, BookingStatus status)
        {
            _context.Bookings.Add(new Booking
            {
                Id = Guid.NewGuid(),
                RoomId = roomId,
                UserId = Guid.NewGuid(),
                StartTime = start,
                EndTime = end,
                Title = "Existing booking",
                AttendeesCount = 1,
                TotalPrice = 0,
                Status = status
            });
            await _context.SaveChangesAsync();
        }
        private Task<bool> InvokeHasOverlapAsync(Guid roomId, DateTime start, DateTime end)
        {
            var method = typeof(BookingService)
                .GetMethod("HasOverlapAsync", BindingFlags.NonPublic | BindingFlags.Instance)
                ?? throw new InvalidOperationException("HasOverlapAsync не знайдено — перевір назву методу.");

            return (Task<bool>)method.Invoke(_service, new object[] { roomId, start, end })!;
        }
        public static IEnumerable<object[]> OverlapCases()
        {
            yield return new object[] { -2.0, -1.0, false, "Повністю до існуючого бронювання" };
            yield return new object[] { 1.0, 2.0, false, "Повністю після існуючого бронювання" };
            yield return new object[] { -1.0, 0.0, false, "Суміжне зліва: new.End == existing.Start" };
            yield return new object[] { 1.0, 2.0, false, "Суміжне справа: new.Start == existing.End" };
            yield return new object[] { -0.5, 0.5, true, "Перетинає початок існуючого" };
            yield return new object[] { 0.5, 1.5, true, "Перетинає кінець існуючого" };
            yield return new object[] { -1.0, 2.0, true, "Повністю поглинає існуюче" };
            yield return new object[] { 0.25, 0.75, true, "Повністю всередині існуючого" };
            yield return new object[] { 0.0, 1.0, true, "Точний збіг інтервалів" };
        }

        [Theory]
        [MemberData(nameof(OverlapCases))]
        public async Task HasOverlapAsync_VariousIntervals_ReturnsExpectedResult(
            double startOffsetHours, double endOffsetHours, bool expectedOverlap, string scenario)
        {
            var newStart = ExistingStart.AddHours(startOffsetHours);
            var newEnd = ExistingStart.AddHours(endOffsetHours);

            var result = await InvokeHasOverlapAsync(_roomId, newStart, newEnd);

            Assert.True(result == expectedOverlap,
                $"Сценарій '{scenario}' провалився: очікували {expectedOverlap}, отримали {result}");
        }

        [Fact]
        public async Task HasOverlapAsync_AdjacentRightBoundary_ReturnsFalse()
        {
            var result = await InvokeHasOverlapAsync(_roomId, ExistingEnd, ExistingEnd.AddHours(1));

            Assert.False(result);
        }

        [Fact]
        public async Task HasOverlapAsync_SameTimeDifferentRoom_ReturnsFalse()
        {
            var result = await InvokeHasOverlapAsync(_otherRoomId, ExistingStart, ExistingEnd);

            Assert.False(result);
        }

        [Fact]
        public async Task HasOverlapAsync_OverlapsButExistingBookingCancelled_ReturnsFalse()
        {
            var cancelledRoomId = Guid.NewGuid();
            await SeedBookingAsync(cancelledRoomId, ExistingStart, ExistingEnd, BookingStatus.Cancelled);

            var result = await InvokeHasOverlapAsync(cancelledRoomId, ExistingStart.AddMinutes(30), ExistingEnd.AddMinutes(30));

            Assert.False(result);
        }

        [Fact]
        public async Task HasOverlapAsync_NoBookingsAtAll_ReturnsFalse()
        {
            var emptyRoomId = Guid.NewGuid();

            var result = await InvokeHasOverlapAsync(emptyRoomId, ExistingStart, ExistingEnd);

            Assert.False(result);
        }
    }
}
