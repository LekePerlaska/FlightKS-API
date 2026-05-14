namespace FlightKS.Helpers;

public static class EnumParse
{
    public static T SnakeCase<T>(string value, T fallback) where T : struct, Enum =>
        TrySnakeCase<T>(value, out var result) ? result : fallback;

    public static bool TrySnakeCase<T>(string? value, out T result) where T : struct, Enum
    {
        result = default;
        if (string.IsNullOrWhiteSpace(value)) return false;

        var normalized = value.Replace("_", string.Empty).Replace("-", string.Empty);
        return Enum.TryParse(normalized, ignoreCase: true, out result);
    }
}
