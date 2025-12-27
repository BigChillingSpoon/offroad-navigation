namespace Offroad.Core;

public sealed record Error(ErrorType Type, string Message)
{
    public static Error NotFound(string entity, object id) =>
        new(ErrorType.NotFound, $"{entity} with id '{id}' was not found.");

    public static Error Validation(string message) =>
        new(ErrorType.Validation, message);

    public static Error Conflict(string message) =>
        new(ErrorType.Conflict, message);

    public static Error Unauthorized(string message = "Unauthorized access.") =>
        new(ErrorType.Unauthorized, message);

    public static Error Forbidden(string message = "Access denied.") =>
        new(ErrorType.Forbidden, message);

    public static Error Internal(string message = "An unexpected error occurred.") =>
        new(ErrorType.Internal, message);
}
