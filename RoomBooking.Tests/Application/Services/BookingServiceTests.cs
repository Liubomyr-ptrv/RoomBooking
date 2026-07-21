using Microsoft.Extensions.Logging;
using Moq;
using RoomBooking.Application.Abstractions.Db;
using RoomBooking.Application.Abstractions.Services;
using RoomBooking.Application.DTOs.Booking;
using RoomBooking.Application.Services;
using RoomBooking.Domain.Entities;
using RoomBooking.Domain.Enums;
using Xunit;

namespace RoomBooking.Tests.Application.Services
{
    public class BookingServiceOverlapTests : IAsyncLifetime
    {
        private readonly FakeAppDbContext _context;
        private readonly Mock<IRoomService> _roomServiceMock;
        private readonly BookingService _sut;

        private readonly Guid _roomId = Guid.NewGuid();
        private readonly Guid _userId = Guid.NewGuid();

      
        private static readonly DateTime AnchorStart = DateTime.UtcNow.Date.AddDays(2).AddHours(9);

        public BookingServiceOverlapTests()
        {
            _context = FakeAppDbContextFactory.Create();
            _roomServiceMock = new Mock<IRoomService>();
            _roomServiceMock
                .Setup(x => x.InvalidateAvailabilityCacheAsync(It.IsAny<Guid>()))
                .Returns(Task.CompletedTask);

            _sut = new BookingService(_context, _roomServiceMock.Object);
        }

        public async Task InitializeAsync()
        {
            _context.Rooms.Add(new Room
            {
                Id = _roomId,
                Name = "Test Room",
                Capacity = 10,
                PricePerHour = 100,
                IsActive = true,
                Location = "Kyiv, Office A1"
            });

            await _context.SaveChangesAsync();
        }

        public Task DisposeAsync() => Task.CompletedTask;

        private async Task<Booking> SeedBookingAsync(DateTime start, DateTime end, BookingStatus status = BookingStatus.Confirmed)
        {
            var booking = new Booking
            {
                Id = Guid.NewGuid(),
                RoomId = _roomId,
                UserId = Guid.NewGuid(),
                StartTime = start,
                EndTime = end,
                Title = "Existing booking",
                AttendeesCount = 2,
                TotalPrice = 100,
                Status = status
            };

            _context.Bookings.Add(booking);
            await _context.SaveChangesAsync();

            return booking;
        }

        private CreateBookingModel BuildRequest(DateTime start, DateTime end) => new()
        {
            RoomId = _roomId,
            StartTime = start,
            EndTime = end,
            Title = "New booking",
            AttendeesCount = 2,
            Notes = null
        };

        public static IEnumerable<object[]> OverlappingCases()
        {
            var existingStart = AnchorStart;
            var existingEnd = AnchorStart.AddHours(2);

           
            yield return new object[] { existingStart, existingEnd };

           
            yield return new object[] { existingStart.AddMinutes(30), existingEnd.AddMinutes(-30) };

           
            yield return new object[] { existingStart.AddMinutes(-30), existingEnd.AddMinutes(30) };

          
            yield return new object[] { existingStart.AddMinutes(-30), existingStart.AddMinutes(30) };

            yield return new object[] { existingEnd.AddMinutes(-30), existingEnd.AddMinutes(30) };
        }

        [Theory]
        [MemberData(nameof(OverlappingCases))]
        public async Task CreateBookingAsync_WhenTimeOverlapsExistingConfirmedBooking_ReturnsConflict(
            DateTime newStart, DateTime newEnd)
        {
            await SeedBookingAsync(AnchorStart, AnchorStart.AddHours(2));

            var result = await _sut.CreateBookingAsync(BuildRequest(newStart, newEnd), _userId);

            Assert.False(result.Succeeded);
            Assert.Equal(ErrorType.Conflict, result.ErrorType);
        }

        [Fact]
        public async Task CreateBookingAsync_WhenNewBookingEndsExactlyWhenExistingStarts_NoOverlap_ReturnsSuccess()
        {
            var existingStart = AnchorStart;
            var existingEnd = AnchorStart.AddHours(2);
            await SeedBookingAsync(existingStart, existingEnd);

            var request = BuildRequest(existingStart.AddHours(-1), existingStart);

            var result = await _sut.CreateBookingAsync(request, _userId);

            Assert.True(result.Succeeded);
        }

        [Fact]
        public async Task CreateBookingAsync_WhenNewBookingStartsExactlyWhenExistingEnds_NoOverlap_ReturnsSuccess()
        {
            var existingStart = AnchorStart;
            var existingEnd = AnchorStart.AddHours(2);
            await SeedBookingAsync(existingStart, existingEnd);

            var request = BuildRequest(existingEnd, existingEnd.AddHours(1));

            var result = await _sut.CreateBookingAsync(request, _userId);

            Assert.True(result.Succeeded);
        }

        [Fact]
        public async Task CreateBookingAsync_WhenNewBookingCompletelyBeforeExisting_ReturnsSuccess()
        {
            var existingStart = AnchorStart.AddHours(5);
            var existingEnd = AnchorStart.AddHours(7);
            await SeedBookingAsync(existingStart, existingEnd);

            var request = BuildRequest(AnchorStart, AnchorStart.AddHours(1));

            var result = await _sut.CreateBookingAsync(request, _userId);

            Assert.True(result.Succeeded);
        }

        [Fact]
        public async Task CreateBookingAsync_WhenNewBookingCompletelyAfterExisting_ReturnsSuccess()
        {
            var existingStart = AnchorStart;
            var existingEnd = AnchorStart.AddHours(1);
            await SeedBookingAsync(existingStart, existingEnd);

            var request = BuildRequest(AnchorStart.AddHours(5), AnchorStart.AddHours(6));

            var result = await _sut.CreateBookingAsync(request, _userId);

            Assert.True(result.Succeeded);
        }

        [Fact]
        public async Task CreateBookingAsync_WhenOverlappingBookingIsCancelled_IgnoresItAndReturnsSuccess()
        {
            
            await SeedBookingAsync(AnchorStart, AnchorStart.AddHours(2), BookingStatus.Cancelled);

            var request = BuildRequest(AnchorStart, AnchorStart.AddHours(2));

            var result = await _sut.CreateBookingAsync(request, _userId);

            Assert.True(result.Succeeded);
        }

        [Fact]
        public async Task CreateBookingAsync_WithMultipleExistingBookings_NoOverlapWithAny_ReturnsSuccess()
        {
            await SeedBookingAsync(AnchorStart, AnchorStart.AddHours(1));
            await SeedBookingAsync(AnchorStart.AddHours(3), AnchorStart.AddHours(4));
            await SeedBookingAsync(AnchorStart.AddHours(6), AnchorStart.AddHours(7));

            var request = BuildRequest(AnchorStart.AddHours(1.5), AnchorStart.AddHours(2.5));

            var result = await _sut.CreateBookingAsync(request, _userId);

            Assert.True(result.Succeeded);
        }

        [Fact]
        public async Task CreateBookingAsync_WithMultipleExistingBookings_OverlapsOneOfThem_ReturnsConflict()
        {
            await SeedBookingAsync(AnchorStart, AnchorStart.AddHours(1));
            await SeedBookingAsync(AnchorStart.AddHours(3), AnchorStart.AddHours(4));
            await SeedBookingAsync(AnchorStart.AddHours(6), AnchorStart.AddHours(7));

         
            var request = BuildRequest(AnchorStart.AddHours(3.5), AnchorStart.AddHours(5));

            var result = await _sut.CreateBookingAsync(request, _userId);

            Assert.False(result.Succeeded);
            Assert.Equal(ErrorType.Conflict, result.ErrorType);
        }

        [Fact]
        public async Task CreateBookingAsync_OverlapCheckIsScopedToRoom_DifferentRoomSameTime_ReturnsSuccess()
        {
            var otherRoomId = Guid.NewGuid();
            _context.Rooms.Add(new Room
            {
                Id = otherRoomId,
                Name = "Other Room",
                Capacity = 5,
                PricePerHour = 50,
                IsActive = true,
                Location = "Kyiv, Office A1"
            });
            await _context.SaveChangesAsync();

            _context.Bookings.Add(new Booking
            {
                Id = Guid.NewGuid(),
                RoomId = otherRoomId,
                UserId = Guid.NewGuid(),
                StartTime = AnchorStart,
                EndTime = AnchorStart.AddHours(2),
                Title = "Booking in another room",
                AttendeesCount = 1,
                TotalPrice = 50,
                Status = BookingStatus.Confirmed
            });
            await _context.SaveChangesAsync();

            // Той самий час, але інша кімната — перетину бути не повинно.
            var request = BuildRequest(AnchorStart, AnchorStart.AddHours(2));

            var result = await _sut.CreateBookingAsync(request, _userId);

            Assert.True(result.Succeeded);
        }

        [Fact]
        public async Task CreateBookingAsync_WhenNoOverlap_InvalidatesAvailabilityCacheForCorrectRoom()
        {
            var request = BuildRequest(AnchorStart, AnchorStart.AddHours(1));

            var result = await _sut.CreateBookingAsync(request, _userId);

            Assert.True(result.Succeeded);
            _roomServiceMock.Verify(x => x.InvalidateAvailabilityCacheAsync(_roomId), Times.Once);
        }
    }
}
