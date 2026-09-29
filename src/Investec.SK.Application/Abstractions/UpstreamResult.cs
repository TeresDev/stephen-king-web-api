namespace Investec.SK.Application.Abstractions;

/// <summary>
/// Success or a typed failure. Infrastructure never lets an exception cross into Application,
/// so every upstream outcome is representable here.
/// </summary>
public sealed record UpstreamResult<T>
{
    private UpstreamResult(bool isSuccess, T? value, UpstreamFailure? failure)
    {
        IsSuccess = isSuccess;
        Value = value;
        Failure = failure;
    }

    public bool IsSuccess { get; }

    public T? Value { get; }

    public UpstreamFailure? Failure { get; }

    public static UpstreamResult<T> Success(T value) => new(true, value, null);

    public static UpstreamResult<T> Failed(UpstreamFailure failure) => new(false, default, failure);
}
