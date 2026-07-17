using RoomBooking.Domain.Enums;

namespace RoomBooking.Application.Common
{
    public class Result<T>
    {
        public bool Succeeded { get; }
        public T? Data { get;  }
        public string? ErrorMessage { get; }
        public ErrorType ErrorType { get;  }

        private Result(bool succeeded, T? data, string? error, ErrorType errorType)
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
                true, data, null, ErrorType.None
            );
        }
        public static Result<T> Failure(string errorMessage, ErrorType errorType)
        {
            return new Result<T>
            (
                false, default, errorMessage, errorType
            );
        }
    }
}
