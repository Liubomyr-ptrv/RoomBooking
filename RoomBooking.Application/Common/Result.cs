using RoomBooking.Domain.Enums;

namespace RoomBooking.Application.Common
{
    public class Result<T>
    {
        public bool Succeeded { get; }
        public T? Data { get;  }
        public string? ErrorMessage { get; }
        public ExeptionType ErrorType { get;  }

        private Result(bool succeeded, T? data, string? error, ExeptionType errorType)
        {
            Succeeded = succeeded;
            Data = data;
            ErrorMessage = error;
            ErrorType = errorType;
        }
        public static Result<T> Success(T data)
        {
            return new Result<T>
            (
                true, data, null, ExeptionType.None
            );
        }
        public static Result<T> Failure(string errorMessage, ExeptionType errorType)
        {
            return new Result<T>
            (
                false, default, errorMessage, errorType
            );
        }
    }
}
