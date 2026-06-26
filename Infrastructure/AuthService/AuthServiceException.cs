using System.Net;

namespace Infrastructure.AuthService;

public class AuthServiceException : Exception
{
    public AuthServiceException(HttpStatusCode statusCode, string? errorCode = null)
        : base(BuildMessage(statusCode, errorCode))
    {
        StatusCode = statusCode;
        ErrorCode = errorCode;
    }

    public HttpStatusCode StatusCode { get; }
    public string? ErrorCode { get; }

    private static string BuildMessage(HttpStatusCode statusCode, string? errorCode)
    {
        return string.IsNullOrWhiteSpace(errorCode)
            ? $"Auth service request failed with status {(int)statusCode}."
            : $"Auth service request failed with status {(int)statusCode} and error '{errorCode}'.";
    }
}
