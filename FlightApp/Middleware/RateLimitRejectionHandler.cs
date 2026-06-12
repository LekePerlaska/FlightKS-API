using System.Diagnostics;
using System.Globalization;
using System.Threading.RateLimiting;
using FlightKS.Exceptions;
using Microsoft.AspNetCore.RateLimiting;

namespace FlightKS.Middleware;

public static class RateLimitRejectionHandler
{
    public static async ValueTask OnRejected(OnRejectedContext context, CancellationToken cancellationToken)
    {
        var httpContext = context.HttpContext;

        // Use lease metadata when available; fall back to 60s (the max window across all tiers).
        // The built-in PartitionedRateLimiter does not always propagate inner-lease RetryAfter.
        var retrySeconds = context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter)
            ? (int)Math.Ceiling(retryAfter.TotalSeconds)
            : 60;
        httpContext.Response.Headers.RetryAfter = retrySeconds.ToString(CultureInfo.InvariantCulture);

        httpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;

        var error = new ErrorResponse(
            Type: "https://httpstatuses.io/429",
            Title: "Too Many Requests",
            Status: 429,
            Code: "rate_limit_exceeded",
            Detail: "You have exceeded the request rate limit. Please slow down and try again.",
            Instance: httpContext.Request.Path,
            TraceId: Activity.Current?.Id ?? httpContext.TraceIdentifier);

        // Pass content type directly — WriteAsJsonAsync overwrites ContentType if set beforehand.
        await httpContext.Response.WriteAsJsonAsync(error, options: null, contentType: "application/problem+json", cancellationToken);
    }
}
