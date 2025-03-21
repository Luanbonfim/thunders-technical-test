namespace Thunders.TechTest.ApiService.Models;

public class OperationResult<T>
{
    public bool IsSuccess { get; private set; }
    public T? Data { get; private set; }
    public string? Message { get; private set; }

    private OperationResult(bool isSuccess, T? data, string? message)
    {
        IsSuccess = isSuccess;
        Data = data;
        Message = message;
    }

    public static OperationResult<T> Success(T data)
    {
        return new OperationResult<T>(true, data, null);
    }

    public static OperationResult<T> Failure(string message)
    {
        return new OperationResult<T>(false, default, message);
    }
}