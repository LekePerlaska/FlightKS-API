using FlightKS.Exceptions;
using FlightKS.Validation;
using Microsoft.AspNetCore.Http.Metadata;

namespace FlightKS.Endpoints;

public static class EndpointExtensions
{
    private static readonly string ProblemJson = "application/problem+json";

    /// <summary>
    /// Documents the standard ErrorResponse envelope for all common error codes.
    /// Applied once at the /api/v1 group level so Scalar shows the error shape on every endpoint.
    /// </summary>
    public static TBuilder WithStandardErrors<TBuilder>(this TBuilder builder)
        where TBuilder : IEndpointConventionBuilder
    {
        foreach (var status in new[] { 400, 401, 403, 404, 409, 422, 429, 500 })
        {
            builder.WithMetadata(new ProducesResponseTypeMetadata(status, typeof(ErrorResponse), [ProblemJson]));
        }
        return builder;
    }

    /// <summary>
    /// Adds FluentValidation for the given DTO type. No-op if no IValidator&lt;T&gt; is registered.
    /// </summary>
    public static RouteHandlerBuilder WithValidation<T>(this RouteHandlerBuilder builder)
        => builder.AddEndpointFilter<ValidationFilter<T>>();
}
