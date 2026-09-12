namespace Microsoft.Extensions.Localization.Json;

public sealed class JsonLocalizationOptions
{
    /// <summary>Folder searched besides the file sitting next to the type. Defaults to "Resources".</summary>
    public string ResourcesPath { get; set; } = "Resources";

    /// <summary>Culture to fall back to before the culture-less file, such as "en".</summary>
    public string? DefaultCulture { get; set; }
}
