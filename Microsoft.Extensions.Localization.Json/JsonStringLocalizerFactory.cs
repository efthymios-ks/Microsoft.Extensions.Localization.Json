using System.Collections.Concurrent;
using System.Reflection;
using Microsoft.Extensions.Localization.Json.Internal;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Localization.Json;

public sealed class JsonStringLocalizerFactory : IStringLocalizerFactory
{
    private readonly JsonResourceCatalog _catalog;
    private readonly ConcurrentDictionary<(Assembly Assembly, string BaseName), JsonStringLocalizer> _localizers = new();

    public JsonStringLocalizerFactory(IOptions<JsonLocalizationOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);

        _catalog = new JsonResourceCatalog(options);
    }

    public IStringLocalizer Create(Type resourceSource)
    {
        ArgumentNullException.ThrowIfNull(resourceSource);

        return Create(resourceSource.Assembly, BaseNameOf(resourceSource));
    }

    /// <summary>
    /// <paramref name="location"/> names the assembly to read from, empty for the entry assembly.
    /// An empty <paramref name="baseName"/> asks for that assembly's shared file.
    /// </summary>
    public IStringLocalizer Create(string baseName, string location)
    {
        var assembly = AssemblyOf(location);

        return Create(assembly, ResourceName.Normalize(baseName));
    }

    private JsonStringLocalizer Create(Assembly assembly, string baseName)
        => _localizers.GetOrAdd(
            (assembly, baseName),
            key => new JsonStringLocalizer(_catalog, key.Assembly, key.BaseName)
        );

    private static string BaseNameOf(Type resourceSource)
    {
        var name = resourceSource.FullName ?? resourceSource.Name;
        var assemblyName = resourceSource.Assembly.GetName().Name;

        if (assemblyName is { Length: > 0 } && name.StartsWith($"{assemblyName}.", StringComparison.Ordinal))
        {
            name = name[(assemblyName.Length + 1)..];
        }

        if (name.IndexOf('`', StringComparison.Ordinal) is var tick && tick >= 0)
        {
            name = name[..tick];
        }

        return name.Replace('+', '.');
    }

    private static Assembly AssemblyOf(string location)
    {
        if (string.IsNullOrWhiteSpace(location))
        {
            return Assembly.GetEntryAssembly() ?? Assembly.GetCallingAssembly();
        }

        return Assembly.Load(new AssemblyName(location));
    }
}
