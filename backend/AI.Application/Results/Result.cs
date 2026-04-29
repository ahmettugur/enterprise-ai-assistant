namespace AI.Application.Results;

public class Result<T> : ResultBase
{
    public T? ResultData { get; set; }

    public new static Result<T> Error(string? userMessage = null, string? systemMessage = null, string? errorCode = null)
    {
        return CreateError<Result<T>>(userMessage, systemMessage, errorCode);
    }

    public new static Result<T> Error(Exception ex, string? caller = null, string? userMessage = "İşlem sırasında hata oluştu", string? errorCode = null)
    {
        return CreateError<Result<T>>(ex, caller, userMessage, errorCode);
    }
    
    public static Result<T> ErrorFrom<T2>(T2 otherResult, string? additionalUserMessage = null) where T2 : ResultBase
    {
        return Error(additionalUserMessage + otherResult.UserMessage, otherResult.SystemMessage, otherResult.ErrorCode);
    }

    public static new Result<T> Success(string? userMessage = null, string? systemMessage = null)
    {
        return CreateSuccess<Result<T>>(userMessage, systemMessage);
    }

    public static Result<T> Success(T data, string? userMessage = null, string? systemMessage = null)
    {
        var result = CreateSuccess<Result<T>>(userMessage, systemMessage);
        result.ResultData = data;
        return result;
    }
}
