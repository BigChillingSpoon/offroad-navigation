using Microsoft.AspNetCore.Mvc;
using Offroad.Core;

namespace Offroad.Api.Extensions
{
    public static class ResultExtensions
    {
        public static IActionResult ToActionResult<T>(this Result<T> result, Func<T, IActionResult> onSuccess)
        {
            return result.Match(
                onSuccess,
                err => err.Type switch
                {
                    ErrorType.NotFound => new NotFoundObjectResult(err.Message),
                    ErrorType.Validation => new BadRequestObjectResult(err.Message),
                    ErrorType.Conflict => new ConflictObjectResult(err.Message),
                    ErrorType.Unauthorized => new UnauthorizedObjectResult(err.Message),
                    ErrorType.Forbidden => new ObjectResult(err.Message) { StatusCode = 403 },
                    ErrorType.Timeout => new ObjectResult(err.Message) { StatusCode = 504 },
                    ErrorType.ExternalServiceFailure => new ObjectResult(err.Message) { StatusCode = 502 },
                    _ => new ObjectResult(err.Message) { StatusCode = 500 }
                }
            );
        }
    }
}
