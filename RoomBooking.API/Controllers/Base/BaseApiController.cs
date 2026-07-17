using Microsoft.AspNetCore.Mvc;
using RoomBooking.Domain.Enums;
using System.Net;

namespace RoomBooking.API.Controllers.Base
{
    [ApiController]
    public class BaseApiController : ControllerBase
    {
        protected IActionResult MapError(ErrorType errorType, string? errorMessage)
        {
            return errorType switch
            {
                ErrorType.NotFound => NotFound(errorMessage),
                ErrorType.Conflict => Conflict(errorMessage),
                ErrorType.Validation => BadRequest(errorMessage),
                ErrorType.Forbidden => StatusCode((int)HttpStatusCode.Forbidden, errorMessage),
                ErrorType.Unauthorized => Unauthorized(errorMessage),
                ErrorType.InternalServerError => StatusCode((int)HttpStatusCode.InternalServerError, errorMessage),
                _ => BadRequest(errorMessage)
            };
        }
    }
}
