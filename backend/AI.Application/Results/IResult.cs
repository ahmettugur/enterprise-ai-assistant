namespace AI.Application.Results;

public interface IResult
{
    string Result { get; set; }
    string ErrorCode { get; set; }
    string SystemMessage { get; set; }
    string UserMessage { get; set; }
    bool IsSucceed { get; }
}
