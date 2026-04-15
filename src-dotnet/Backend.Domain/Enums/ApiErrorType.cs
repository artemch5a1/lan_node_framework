namespace Backend.Domain.Enums;

public enum ApiErrorType
{
    Ok = 200,

    // Ошибки клиента (4xx)
    BadRequest = 400,
    Unauthorized = 401,
    Forbidden = 403,
    NotFound = 404,
    Conflict = 409,
    UnprocessableEntity = 422,
    TooManyRequests = 429,

    // Ошибки сервера (5xx)
    InternalServerError = 500,
    ServiceUnavailable = 503,
    GatewayTimeout = 504,
}
