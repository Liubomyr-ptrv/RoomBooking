namespace RoomBooking.Application.Settings
{
    public class JwtConfigurationOptions
    {
        public required string Key { get; set; }
        public required string Audience { get; set; }
        public required string Issuer { get; set; }
        public int ExpirationInHours { get; set; } = 1;
    }
}
