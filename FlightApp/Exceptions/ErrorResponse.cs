namespace FlightKS.Exceptions;

public sealed record ErrorResponse(
    string Type,
    string Title,
    int Status,
    string Code,
    string Detail,
    string Instance,
    string TraceId,
    IReadOnlyDictionary<string, string[]>? Errors = null);
