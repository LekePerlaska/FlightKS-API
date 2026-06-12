using System.Diagnostics;
using FlightKS.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Net.Http.Headers;
using Npgsql;

namespace FlightKS.Middleware;

public sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger, IHostEnvironment env)
    : IExceptionHandler
{
    private const string ProblemJsonMediaType = "application/problem+json";

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is OperationCanceledException)
            return false;

        var (status, code, title, detail) = exception switch
        {
            AppException app => (app.StatusCode, app.Code, StatusTitle(app.StatusCode), app.Message),
            UnauthorizedAccessException => (401, "unauthorized", "Unauthorized", "Authentication is required."),
            DbUpdateException due when IsUniqueConstraint(due) =>
                (409, "conflict", "Conflict", "A resource with the same unique key already exists."),
            _ => (500, "internal_error", "Internal Server Error", InternalDetail(exception))
        };

        if (status == 500)
            logger.LogError(exception, "Unhandled exception: {Message}", exception.Message);

        var response = new ErrorResponse(
            Type: $"https://httpstatuses.io/{status}",
            Title: title,
            Status: status,
            Code: code,
            Detail: detail,
            Instance: httpContext.Request.Path,
            TraceId: Activity.Current?.Id ?? httpContext.TraceIdentifier,
            Errors: exception is ValidationException ve ? ve.Errors : null);

        httpContext.Response.StatusCode = status;
        httpContext.Response.Headers[HeaderNames.ContentType] = ProblemJsonMediaType;
        await httpContext.Response.WriteAsJsonAsync(response, cancellationToken);
        return true;
    }

    // Postgres error code 23505 = unique_violation
    private static bool IsUniqueConstraint(DbUpdateException ex) =>
        ex.InnerException is PostgresException { SqlState: "23505" };

    private string InternalDetail(Exception ex) =>
        env.IsDevelopment() ? ex.ToString() : "An unexpected error occurred. Please try again later.";

    private static string StatusTitle(int status) => status switch
    {
        400 => "Bad Request",
        403 => "Forbidden",
        404 => "Not Found",
        409 => "Conflict",
        422 => "Unprocessable Entity",
        _ => "Error"
    };
}
