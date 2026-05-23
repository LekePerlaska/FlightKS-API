using FlightKS.Mappers;
using FlightKS.Services.Interfaces;

namespace FlightKS.Endpoints.V1;

public static class BaggageOptionsEndpoints
{
    public static IEndpointRouteBuilder MapBaggageOptionsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/baggage-options").WithTags("BaggageOptions");

        group.MapGet("/", GetAll).WithName("GetBaggageOptions");

        return app;
    }

    private static async Task<IResult> GetAll(IBaggageOptionService baggage, CancellationToken cancellationToken)
    {
        var list = await baggage.GetAllAsync(cancellationToken);
        return TypedResults.Ok(list.Select(b => b.ToDto()));
    }
}
