namespace RoomBooking.Domain.Enums
{
    public enum ExeptionType
    {
        None,
        UserNotFound,
        InvalidEmailOrPassword,
        UserAlreadyExists,
        WeakPassword,
        UserLockedOut,
        InvalidEmailFormat,
        IdentityError,
        Unknown
    }
}
