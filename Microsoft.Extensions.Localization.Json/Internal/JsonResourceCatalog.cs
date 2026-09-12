using System.Collections.Concurrent;
using System.Globalization;
using System.Reflection;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Localization.Json.Internal;

internal sealed class JsonResourceCatalog(IOptions<JsonLocalizationOptions> options)
{
    private readonly JsonLocalizationOptions _options = options.Value;
    private readonly ConcurrentDictionary<ResourceKey, IReadOnlyDictionary<string, string>> _entries = new();
    private readonly ConcurrentDictionary<Assembly, string[]> _manifestNames = new();

    public string? Find(Assembly assembly, string baseName, CultureInfo culture, string name)
    {
        foreach (var candidate in Chain(culture))
        {
            if (EntriesOf(assembly, baseName, candidate).TryGetValue(name, out var value))
            {
                return value;
            }
        }

        return null;
    }

    public IReadOnlyDictionary<string, string> Merged(
        Assembly assembly,
        string baseName,
        CultureInfo culture,
        bool includeParentCultures
    )
    {
        var merged = new Dictionary<string, string>(StringComparer.Ordinal);
        var cultures = includeParentCultures ? Chain(culture) : [culture];

        foreach (var candidate in cultures.Reverse())
        {
            foreach (var entry in EntriesOf(assembly, baseName, candidate))
            {
                merged[entry.Key] = entry.Value;
            }
        }

        return merged;
    }

    public IReadOnlyDictionary<string, string> EntriesOf(Assembly assembly, string baseName, CultureInfo culture)
        => _entries.GetOrAdd(new ResourceKey(assembly, baseName, culture.Name), Load);

    private CultureInfo[] Chain(CultureInfo culture)
    {
        var chain = new List<CultureInfo>();

        for (var candidate = culture; !string.IsNullOrEmpty(candidate.Name); candidate = candidate.Parent)
        {
            chain.Add(candidate);
        }

        if (_options.DefaultCulture is { Length: > 0 } defaultCulture)
        {
            var fallback = CultureInfo.GetCultureInfo(defaultCulture);

            if (!chain.Any(candidate => string.Equals(candidate.Name, fallback.Name, StringComparison.OrdinalIgnoreCase)))
            {
                chain.Add(fallback);
            }
        }

        chain.Add(CultureInfo.InvariantCulture);

        return [.. chain];
    }

    private IReadOnlyDictionary<string, string> Load(ResourceKey key)
    {
        var resourceName = FindResourceName(key);

        if (resourceName is null)
        {
            return ReadOnlyDictionary.Empty;
        }

        using var stream = key.Assembly.GetManifestResourceStream(resourceName);

        if (stream is null)
        {
            return ReadOnlyDictionary.Empty;
        }

        using var document = JsonDocument.Parse(stream);

        var entries = new Dictionary<string, string>(StringComparer.Ordinal);

        Flatten(document.RootElement, prefix: string.Empty, entries);

        return entries;
    }

    /// <summary>Matched on the tail of the manifest name, so the root namespace need not be guessed.</summary>
    private string? FindResourceName(ResourceKey key)
    {
        var names = _manifestNames.GetOrAdd(key.Assembly, assembly => assembly.GetManifestResourceNames());

        foreach (var suffix in Suffixes(key))
        {
            // Several files can end the same way — Pages/Index.en.json and Resources/Pages/Index.en.json
            // both end in ".Pages.Index.en.json". The shortest is the one with nothing in front of it.
            var match = names
                .Where(name => name.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                .OrderBy(name => name.Length)
                .FirstOrDefault();

            if (match is not null)
            {
                return match;
            }
        }

        return null;
    }

    private IEnumerable<string> Suffixes(ResourceKey key)
    {
        var culture = key.CultureName.Length == 0 ? string.Empty : $".{key.CultureName}";
        var resourcesPath = ResourceName.Normalize(_options.ResourcesPath);

        if (key.BaseName.Length > 0)
        {
            // The file sitting next to the type is the more specific of the two, so it wins.
            yield return $".{key.BaseName}{culture}.json";

            if (resourcesPath.Length > 0)
            {
                yield return $".{resourcesPath}.{key.BaseName}{culture}.json";
            }
        }
        else if (resourcesPath.Length > 0)
        {
            // The shared file of an assembly, which has no base name of its own.
            yield return $".{resourcesPath}{culture}.json";
        }
        else if (culture.Length > 0)
        {
            yield return $"{culture}.json";
        }
    }

    /// <summary>Nested objects are read as dotted keys, so a nested "form": { "title" } is "form.title".</summary>
    private static void Flatten(JsonElement element, string prefix, Dictionary<string, string> entries)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var property in element.EnumerateObject())
                {
                    Flatten(property.Value, Join(prefix, property.Name), entries);
                }

                break;

            case JsonValueKind.Array:
                var index = 0;

                foreach (var item in element.EnumerateArray())
                {
                    Flatten(item, Join(prefix, index.ToString(CultureInfo.InvariantCulture)), entries);
                    index++;
                }

                break;

            case JsonValueKind.Null or JsonValueKind.Undefined:
                break;

            default:
                if (prefix.Length > 0)
                {
                    entries[prefix] = element.ValueKind is JsonValueKind.String
                        ? element.GetString()!
                        : element.GetRawText();
                }

                break;
        }
    }

    private static string Join(string prefix, string name)
        => prefix.Length == 0 ? name : $"{prefix}.{name}";

    private sealed record ResourceKey(Assembly Assembly, string BaseName, string CultureName);

    private static class ReadOnlyDictionary
    {
        public static readonly IReadOnlyDictionary<string, string> Empty
            = new Dictionary<string, string>(StringComparer.Ordinal);
    }
}
