using Backend.Domain.Enums;

namespace Backend.Domain.Common;

/// <summary>
/// Сообщение для возврата при необработанном исключении в сервисах (без раскрытия деталей).
/// </summary>
public static class ServiceErrorMessages
{
    public const string Generic = "Произошла внутренняя ошибка. Попробуйте позже.";
}

public class Result
{
    public bool IsSuccess { get; }

    public bool IsFailure => !IsSuccess;

    public string Message { get; } = string.Empty;

    public ApiErrorType ErrorType { get; set; }

    protected Result(bool isSucces, string message, ApiErrorType errorType)
    {
        IsSuccess = isSucces;
        Message = message;
        ErrorType = errorType;
    }

    public static Result Success() => new Result(true, string.Empty, ApiErrorType.Ok);

    public static Result Failure(string errorMessage, ApiErrorType errorType) =>
        new Result(false, errorMessage, errorType);

    public static Result Failure(Exception ex)
    {
        return new Result(false, ex.Message, ApiErrorType.InternalServerError);
    }
}

public class Result<T> : Result
{
    protected Result(T? value, bool isSucces, string message, ApiErrorType errorType)
        : base(isSucces, message, errorType)
    {
        _value = value;
    }

    private T? _value { get; }

    public T Value => _value ?? throw new Exception("Попытка получить пустое значение");

    public static Result<T> Success(T? value) =>
        new Result<T>(value, true, string.Empty, ApiErrorType.Ok);

    public static new Result<T> Failure(string errorMessage, ApiErrorType errorType) =>
        new Result<T>(default, false, errorMessage, errorType);

    public static Result<T> FailureNotFound(string nameOfObject) =>
        new Result<T>(default, false, $"{nameOfObject} не найден", ApiErrorType.NotFound);

    public static new Result<T> Failure(Exception ex)
    {
        return new Result<T>(default, false, ex.Message, ApiErrorType.InternalServerError);
    }
}
