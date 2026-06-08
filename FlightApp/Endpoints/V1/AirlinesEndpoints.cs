using FlightKS.Mappers;
using FlightKS.Services.Interfaces;

namespace FlightKS.Endpoints.V1;

public static class AirlinesEndpoints
{
    public static IEndpointRouteBuilder MapAirlinesEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/airlines").WithTags("Airlines");

        group.MapGet("/", GetAll).WithName("GetAirlines");

        return app;
    }

    private static async Task<IResult> GetAll(IAirlineService airlines, CancellationToken cancellationToken)
    {
        var list = await airlines.GetAllAsync(cancellationToken);
        return TypedResults.Ok(list.Select(a => a.ToDto()));
    }
}
