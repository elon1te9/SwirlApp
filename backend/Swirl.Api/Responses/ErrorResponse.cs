using System.Text.Json.Serialization;

namespace Swirl.Api.Responses;

public class ErrorResponse
{
    public ErrorResponse(ErrorDetails error)
    {
        Error = error;
    }

    public ErrorDetails Error { get; }
}

public class ErrorDetails
{
    public ErrorDetails(
        string code,
        string message,
        IDictionary<string, string[]>? details = null)
    {
        Code = code;
        Message = message;
        Details = details;
    }

    public string Code { get; }

    public string Message { get; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IDictionary<string, string[]>? Details { get; }
}
