using Microsoft.AspNetCore.Mvc;
using RoomBooking.Domain.Enums;
using System.Net;

namespace RoomBooking.API.Controllers.Base
{
    [Route("api/[controller]")]
    [ApiController]
    public class BaseApiController : ControllerBase
    {
        protected IActionResult MapError(ExeptionType errorType, string? errorMessage)
        {
            return errorType switch
            {
                ExeptionType.NotFound => NotFound(errorMessage),
                ExeptionType.Conflict => Conflict(errorMessage),
                ExeptionType.Validation => BadRequest(errorMessage),
                ExeptionType.Forbidden => StatusCode((int)HttpStatusCode.Forbidden, errorMessage),
                ExeptionType.Unauthorized => Unauthorized(errorMessage),
                ExeptionType.InternalServerError => StatusCode((int)HttpStatusCode.InternalServerError, errorMessage),
                _ => BadRequest(errorMessage)
            };
        }
    }
}
