using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using ReverseProxy.WebApi.Application.Common;

namespace ReverseProxy.WebApi.Middleware;

public sealed partial class ApiExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<ApiExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var apiProblem = exception as ApiProblemException;
        var statusCode = apiProblem?.StatusCode ?? StatusCodes.Status500InternalServerError;
        if (apiProblem is null)
        {
            LogUnhandledException(logger, exception);
        }
        else
        {
            LogHandledException(logger, apiProblem.ErrorCode, statusCode);
        }

        httpContext.Response.StatusCode = statusCode;
        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = apiProblem?.Title ?? "An unexpected error occurred",
            Detail = apiProblem?.Message ?? "The request could not be completed.",
            Instance = httpContext.Request.Path
        };
        problemDetails.Extensions["errorCode"] = apiProblem?.ErrorCode ?? "unexpected_error";

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = problemDetails
        });
    }

    [LoggerMessage(1, LogLevel.Error, "Unhandled API exception.")]
    private static partial void LogUnhandledException(ILogger logger, Exception exception);

    [LoggerMessage(2, LogLevel.Warning, "Handled API problem {ErrorCode} with status {StatusCode}.")]
    private static partial void LogHandledException(ILogger logger, string errorCode, int statusCode);
}