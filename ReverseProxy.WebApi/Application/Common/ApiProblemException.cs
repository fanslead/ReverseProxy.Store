namespace ReverseProxy.WebApi.Application.Common;

public sealed class ApiProblemException : Exception
{
    public ApiProblemException(int statusCode, string errorCode, string title, string detail)
        : base(detail)
    {
        StatusCode = statusCode;
        ErrorCode = errorCode;
        Title = title;
    }

    public int StatusCode { get; }

    public string ErrorCode { get; }

    public string Title { get; }
}