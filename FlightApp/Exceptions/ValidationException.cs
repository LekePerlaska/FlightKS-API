namespace FlightKS.Exceptions;

public sealed class ValidationException : AppException
{
    public IReadOnlyDictionary<string, string[]> Errors { get; }

    public ValidationException(string message, IReadOnlyDictionary<string, string[]> errors)
        : base(message, 400, "validation_error")
    {
        Errors = errors;
    }

    public ValidationException(string field, string error)
        : this("One or more validation errors occurred.", new Dictionary<string, string[]>
        {
            [field] = [error]
        }) { }
}
