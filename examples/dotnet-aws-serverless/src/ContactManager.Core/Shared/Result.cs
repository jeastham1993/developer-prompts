namespace ContactManager.Core.Shared;

public abstract record Result<T>
{
    public abstract bool IsSuccess { get; }
    public virtual bool IsFailure => !IsSuccess;

    public static Result<T> Success(T value) => new Success<T>(value);
    public static Result<T> Failure(string error, Exception? exception = null) => new Failure<T>(error, exception);
}

public sealed record Success<T>(T Value) : Result<T>
{
    public override bool IsSuccess => true;
}

public sealed record Failure<T>(string Error, Exception? Exception = null) : Result<T>
{
    public override bool IsSuccess => false;
}

public static class ResultExtensions
{
    public static Result<TResult> Map<T, TResult>(this Result<T> result, Func<T, TResult> mapper)
    {
        return result switch
        {
            Success<T> success => Result<TResult>.Success(mapper(success.Value)),
            Failure<T> failure => Result<TResult>.Failure(failure.Error, failure.Exception),
            _ => throw new InvalidOperationException("Unknown result type")
        };
    }

    public static async Task<Result<TResult>> MapAsync<T, TResult>(
        this Result<T> result, 
        Func<T, Task<TResult>> mapper)
    {
        return result switch
        {
            Success<T> success => Result<TResult>.Success(await mapper(success.Value)),
            Failure<T> failure => Result<TResult>.Failure(failure.Error, failure.Exception),
            _ => throw new InvalidOperationException("Unknown result type")
        };
    }

    public static Result<TResult> FlatMap<T, TResult>(this Result<T> result, Func<T, Result<TResult>> mapper)
    {
        return result switch
        {
            Success<T> success => mapper(success.Value),
            Failure<T> failure => Result<TResult>.Failure(failure.Error, failure.Exception),
            _ => throw new InvalidOperationException("Unknown result type")
        };
    }
}