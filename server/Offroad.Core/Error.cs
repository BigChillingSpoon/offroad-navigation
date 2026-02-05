namespace Offroad.Core;

public sealed record Error(ErrorType Type, string Message)
{
    //404
    public static Error NotFound(string entity, object id) =>
        new(ErrorType.NotFound, $"{entity} with id '{id}' was not found.");

    //4xx
    public static Error Validation(string message) =>
        new(ErrorType.Validation, message);
    //409
    public static Error Conflict(string message) =>
        new(ErrorType.Conflict, message);
    //401
    public static Error Unauthorized(string message = "Unauthorized access.") =>
        new(ErrorType.Unauthorized, message);

    //403
    public static Error Forbidden(string message = "Access denied.") =>
        new(ErrorType.Forbidden, message);

    //500
    public static Error Internal(string message = "An unexpected error occurred.") =>
        new(ErrorType.Internal, message);

    //504
    public static Error Timeout(string message = "Server response took longer than expected.") =>
        new(ErrorType.Timeout, message);

    //5xx 
    public static Error ExternalServiceFailure(string message = "External service is unavailable at the moment.") =>
        new(ErrorType.ExternalServiceFailure, message);
    
}
