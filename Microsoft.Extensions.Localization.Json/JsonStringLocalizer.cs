using System.Globalization;
using System.Reflection;
using Microsoft.Extensions.Localization.Json.Internal;

namespace Microsoft.Extensions.Localization.Json;

/// <summary>
/// Reads its strings from the JSON files embedded in an assembly. The culture is read at lookup
/// time, so one localizer serves every request whatever the current UI culture is.
/// </summary>
public sealed class JsonStringLocalizer : IStringLocalizer
{
    private readonly JsonResourceCatalog _catalog;
    private readonly Assembly _assembly;
    private readonly string _baseName;

    internal JsonStringLocalizer(JsonResourceCatalog catalog, Assembly assembly, string baseName)
    {
        _catalog = catalog;
        _assembly = assembly;
        _baseName = baseName;
    }

    public LocalizedString this[string name]
        => Find(name);

    public LocalizedString this[string name, params object[] arguments]
    {
        get
        {
            ArgumentNullException.ThrowIfNull(arguments);

            var found = Find(name);

            return new LocalizedString(
                name: found.Name,
                value: string.Format(CultureInfo.CurrentCulture, found.Value, arguments),
                resourceNotFound: found.ResourceNotFound,
                searchedLocation: found.SearchedLocation
            );
        }
    }

    private string SearchedLocation
        => _baseName.Length == 0
            ? _assembly.GetName().Name ?? _assembly.FullName ?? string.Empty
            : $"{_assembly.GetName().Name}.{_baseName}";

    public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
        => _catalog
            .Merged(_assembly, _baseName, CultureInfo.CurrentUICulture, includeParentCultures)
            .Select(entry => new LocalizedString(entry.Key, entry.Value, resourceNotFound: false, SearchedLocation));

    private LocalizedString Find(string name)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);

        var value = _catalog.Find(_assembly, _baseName, CultureInfo.CurrentUICulture, name);

        return value is null
            ? new LocalizedString(name, name, resourceNotFound: true, SearchedLocation)
            : new LocalizedString(name, value, resourceNotFound: false, SearchedLocation);
    }
}
