namespace FlightKS.Exceptions;

public abstract class AppException(string message, int statusCode, string code) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
    public string Code { get; } = code;
}
