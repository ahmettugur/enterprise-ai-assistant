namespace AI.Application.Results;

public class ResultBase : IResult
{
    private const string ResultErrorCode = "1";
    private const string ResultSuccessCode = "0";

    public string Result { get; set; } = string.Empty;
    public string ErrorCode { get; set; } = string.Empty;
    public string SystemMessage { get; set; } = string.Empty;
    public string UserMessage { get; set; } = string.Empty;
    
    public bool IsSucceed => string.IsNullOrWhiteSpace(Result) || Result == ResultSuccessCode;

    public static ResultBase Error(string? userMessage = "İşlem sırasında hata oluştu", string? systemMessage = null, string? errorCode = null)
    {
        return CreateError<ResultBase>(userMessage, systemMessage, errorCode);
    }

    public static ResultBase Error(Exception ex, string? caller = null, string? userMessage = "İşlem sırasında hata oluştu", string? errorCode = null)
    {
        return CreateError<ResultBase>(ex, caller, userMessage, errorCode);
    }

    public static ResultBase Success(string? userMessage = null, string? systemMessage = null)
    {
        return CreateSuccess<ResultBase>(userMessage, systemMessage);
    }

    public static ResultBase Error(ResultBase otherResult)
    {
        return CreateError<ResultBase>(otherResult.UserMessage, otherResult.SystemMessage, otherResult.ErrorCode);
    }

    public static T CreateError<T>(string? userMessage = "İşlem sırasında hata oluştu", string? systemMessage = null, string? errorCode = null) where T : IResult, new()
    {
        return new T
        {
            Result = ResultErrorCode,
            ErrorCode = errorCode ?? string.Empty,
            SystemMessage = systemMessage ?? string.Empty,
            UserMessage = userMessage ?? string.Empty
        };
    }

    public static T CreateError<T>(Exception ex, string? caller, string? userMessage = "İşlem sırasında hata oluştu", string? errorCode = null) where T : IResult, new()
    {
        return new T
        {
            Result = ResultErrorCode,
            ErrorCode = errorCode ?? string.Empty,
            SystemMessage = GetSystemExceptionMessage(ex, caller ?? string.Empty),
            UserMessage = userMessage ?? string.Empty
        };
    }

    public static T CreateSuccess<T>(string? userMessage = null, string? systemMessage = null) where T : IResult, new()
    {
        return new T
        {
            Result = ResultSuccessCode,
            ErrorCode = string.Empty,
            SystemMessage = systemMessage ?? string.Empty,
            UserMessage = userMessage ?? string.Empty
        };
    }

    private static string GetSystemExceptionMessage(Exception ex, string caller)
    {
        return $"MethodName:{caller} {Environment.NewLine}Message: {ex.Message} {Environment.NewLine}StackTrace:{ex.StackTrace}";
    }
}
