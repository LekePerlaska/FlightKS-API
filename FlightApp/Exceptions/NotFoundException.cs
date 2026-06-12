namespace FlightKS.Exceptions;

public sealed class NotFoundException(string message) : AppException(message, 404, "not_found");
