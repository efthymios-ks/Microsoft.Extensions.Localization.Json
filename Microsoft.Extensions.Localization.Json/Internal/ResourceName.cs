namespace Microsoft.Extensions.Localization.Json.Internal;

internal static class ResourceName
{
    public static string Normalize(string? name)
        => name is null
            ? string.Empty
            : name
                .Replace('/', '.')
                .Replace('\\', '.')
                .Trim('.');
}
