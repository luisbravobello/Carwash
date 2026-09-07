namespace Carwash.Domain.Common;

public class Result
{
  public bool IsSuccess { get; }
  public string? Error { get; }
  protected Result(bool ok, string? error) { IsSuccess = ok; Error = error; }
  public static Result Success() => new(true, null);
  public static Result Failure(string error) => new(false, error);
}

public class Result<T> : Result
{
  public T? Value { get; }
  private Result(bool ok, T? value, string? error) : base(ok, error) { Value = value; }
  public static Result<T> Success(T value) => new(true, value, null);
  public static new Result<T> Failure(string error) => new(false, default, error);
}
