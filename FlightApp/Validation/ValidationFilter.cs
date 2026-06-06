using FluentValidation;

namespace FlightKS.Validation;

public sealed class ValidationFilter<T> : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var validator = context.HttpContext.RequestServices.GetService<IValidator<T>>();
        if (validator is null)
            return await next(context);

        var argument = context.Arguments.OfType<T>().FirstOrDefault();
        if (argument is null)
            return await next(context);

        var result = await validator.ValidateAsync(argument, context.HttpContext.RequestAborted);
        if (result.IsValid)
            return await next(context);

        var errors = result.Errors
            .GroupBy(f => ToCamelCase(f.PropertyName))
            .ToDictionary(
                g => g.Key,
                g => g.Select(f => f.ErrorMessage).ToArray()
            );

        throw new Exceptions.ValidationException("One or more validation errors occurred.", errors);
    }

    private static string ToCamelCase(string name)
    {
        if (string.IsNullOrEmpty(name)) return name;
        // Handle paths like "ClassPrices[0].Price" → "classPrices[0].price"
        // Split on '.' and camelCase each segment individually
        return string.Join('.', name.Split('.').Select(SegmentToCamelCase));
    }

    private static string SegmentToCamelCase(string segment) =>
        string.IsNullOrEmpty(segment) || char.IsLower(segment[0])
            ? segment
            : char.ToLowerInvariant(segment[0]) + segment[1..];
}
