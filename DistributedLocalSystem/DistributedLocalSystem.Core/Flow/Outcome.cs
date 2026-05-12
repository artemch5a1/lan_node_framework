namespace DistributedLocalSystem.Core.Flow;

/// <summary>Успех или неудача без исключений в контракте (не называется Result).</summary>
public readonly struct Outcome<T>
{
    private readonly T? _value;
    private readonly NetFlowError? _error;

    private Outcome(bool isSuccess, T? value, NetFlowError? error)
    {
        IsSuccess = isSuccess;
        _value = value;
        _error = error;
    }

    public bool IsSuccess { get; }

    public bool IsFailure => !IsSuccess;

    public T Value =>
        IsSuccess ? _value! : throw new InvalidOperationException("Outcome has no value.");

    public NetFlowError Error =>
        IsFailure ? _error! : throw new InvalidOperationException("Outcome has no error.");

    public static Outcome<T> Ok(T value) => new(true, value, null);

    public static Outcome<T> Fail(NetFlowError error) => new(false, default, error);

    public static Outcome<T> Fail(string code, string message) =>
        Fail(new NetFlowError(code, message));

    public static Outcome<T> FromException(string code, Exception exception) =>
        Fail(code, exception.Message);

    public TResult Match<TResult>(
        Func<T, TResult> onSuccess,
        Func<NetFlowError, TResult> onFailure
    ) => IsSuccess ? onSuccess(_value!) : onFailure(_error!);
}
