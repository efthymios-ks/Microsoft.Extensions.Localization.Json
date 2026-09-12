using System.Globalization;

namespace Microsoft.Extensions.Localization.Json.Tests;

/// <summary>Sets the UI culture a lookup reads, and puts back what was there.</summary>
public sealed class CultureScope : IDisposable
{
    private readonly CultureInfo _previous = CultureInfo.CurrentUICulture;

    private CultureScope(string name)
        => CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(name);

    public static CultureScope Use(string name)
        => new(name);

    public void Dispose()
        => CultureInfo.CurrentUICulture = _previous;
}
