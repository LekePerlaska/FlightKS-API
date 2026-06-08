using FlightKS.Models.Dtos.Timezones;
using NodaTime;

namespace FlightKS.Endpoints.V1;

public static class TimezonesEndpoints
{
    public static IEndpointRouteBuilder MapTimezonesEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/timezones", GetAll).WithTags("Timezones").WithName("GetTimezones");
        return app;
    }

    private static IResult GetAll()
    {
        var now = SystemClock.Instance.GetCurrentInstant();

        var timezones = DateTimeZoneProviders.Tzdb.Ids
            .Select(id =>
            {
                var zone = DateTimeZoneProviders.Tzdb[id];
                var offset = zone.GetUtcOffset(now);
                var totalMinutes = offset.Seconds / 60;
                var sign = totalMinutes >= 0 ? "+" : "-";
                var absMinutes = Math.Abs(totalMinutes);
                var offsetStr = $"UTC{sign}{absMinutes / 60:D2}:{absMinutes % 60:D2}";

                var parts = id.Split('/');
                var region = parts[0];
                var city = parts.Length > 1
                    ? parts[^1].Replace("_", " ")
                    : id;

                return new TimezoneDto(id, id, region, city, offsetStr);
            })
            .OrderBy(t => t.Region)
            .ThenBy(t => t.Id)
            .ToList();

        return TypedResults.Ok(timezones);
    }
}

