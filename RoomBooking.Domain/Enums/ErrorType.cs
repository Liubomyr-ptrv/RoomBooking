namespace RoomBooking.Domain.Enums
{
    public enum ErrorType
    {
        None,
        NotFound,
        Validation,
        Forbidden,
        Unauthorized,
        Conflict,
        InternalServerError,
        Unknown
    }
}
