namespace Swirl.Api.Services;

public class ApiException : Exception
{
    public ApiException(
        int statusCode,
        string code,
        string message,
        IDictionary<string, string[]>? details = null)
        : base(message)
    {
        StatusCode = statusCode;
        Code = code;
        Details = details;
    }

    public int StatusCode { get; }

    public string Code { get; }

    public IDictionary<string, string[]>? Details { get; }
}
