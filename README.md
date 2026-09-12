# Microsoft.Extensions.Localization.Json

`IStringLocalizer` backed by JSON files embedded in your assemblies, instead of `.resx`.
```
JsonStringLocalizer.cs           IStringLocalizer over one resource
JsonStringLocalizerFactory.cs    IStringLocalizerFactory: by type, or by base name and assembly
JsonLocalizationOptions.cs       ResourcesPath, DefaultCulture
ServiceCollectionExtensions.cs   AddJsonLocalization()
Internal/JsonResourceCatalog.cs  reads and keeps the files, walks the culture chain
```

## Setting up a JSON file

Two things make a JSON file reachable: **where it sits** and **how the project embeds it**.

```
MyApp/
  Resources/
    en.json          the app's shared strings
    el.json
  Pages/
    IndexPage.cs
    IndexPage.en.json    strings for one type, next to it
    IndexPage.json       the culture-less fallback for that type
```

```xml
<ItemGroup>
  <EmbeddedResource Include="Resources\*.json" WithCulture="false" />
  <EmbeddedResource Include="Pages\*.json" WithCulture="false" />
</ItemGroup>
```

`WithCulture="false"` is the part that is easy to miss. Without it MSBuild reads the `.en` in
`IndexPage.en.json` as a culture, moves the file into a satellite assembly, and it disappears from
the main assembly's manifest — the localizer then finds nothing. Files named only after a culture
(`Resources/en.json`) survive either way, but keep the attribute on both so the rule is one rule.

Set **Build Action → Embedded resource** in the IDE and the same item is written, minus
`WithCulture`; add it by hand in the `.csproj`.

The file itself is a flat or nested object. Nested objects are read as dotted keys, so both of these
answer to `form.title`:

```json
{
  "form.title": "Your details"
}
```

```json
{
  "form": {
    "title": "Your details",
    "fields": [ "First", "Second" ]
  }
}
```

Values that are not strings are read as written — `"count": 3` comes back as `"3"` — and a `null`
counts as missing. Arrays are numbered: `form.fields.0`.

## Registration

```csharp
builder.Services.AddJsonLocalization(options =>
{
    options.ResourcesPath = "Resources";
    options.DefaultCulture = "en";
});
```

| Option | Does | Default |
| --- | --- | --- |
| `ResourcesPath` | folder searched when no file sits next to the type | `Resources` |
| `DefaultCulture` | culture to fall back to before the culture-less file | none |

`AddJsonLocalization` registers the factory, `IStringLocalizer<TResource>`, and a plain
`IStringLocalizer` reading the entry assembly's shared file.

## Reading strings

```csharp
public sealed class IndexPage(IStringLocalizer<IndexPage> localizer)
{
    public string Title => localizer["form.title"];
    public string Greeting(string name) => localizer["greet", name];   // "Hello {0}"
}
```

```csharp
public sealed class Banner(IStringLocalizer localizer)
{
    public string Text => localizer["banner.text"];   // from Resources/{culture}.json
}
```

A key that is nowhere comes back as itself, with `ResourceNotFound` set and `SearchedLocation`
naming the resource that was looked through — nothing throws, so a missing translation never takes
a page down. A file that is not valid JSON does throw, since that is a build mistake rather than a
gap in a translation.

The culture is read at lookup time from `CultureInfo.CurrentUICulture`, so one registration serves
every request. In ASP.NET Core, `app.UseRequestLocalization(...)` is what sets it.

## Which file is read

For `IStringLocalizer<MyApp.Pages.IndexPage>` under culture `el-GR`, with `ResourcesPath` of
`Resources` and `DefaultCulture` of `en`, the localizer looks for the first file that carries the
key, in this order:

```
Pages/IndexPage.el-GR.json      Resources/Pages/IndexPage.el-GR.json
Pages/IndexPage.el.json         Resources/Pages/IndexPage.el.json
Pages/IndexPage.en.json         Resources/Pages/IndexPage.en.json
Pages/IndexPage.json            Resources/Pages/IndexPage.json
```

The plain `IStringLocalizer` has no type to be named after, so it reads the shared file:

```
Resources/el-GR.json → Resources/el.json → Resources/en.json → Resources.json
```

Three things worth knowing about the walk. Across cultures it is **per key**: a Greek file holding
half the strings falls back to English for the rest, one key at a time. Within one culture it is
**per file** — the copy next to the type wins outright, and the one under `ResourcesPath` is not
read at all, so the same type and culture should not be written twice. And matching is done on the
tail of the manifest name, so the assembly's root namespace never has to be guessed — a file under
`Pages/` is found whether the root namespace matches the folder or not.

Asking the factory directly reaches any assembly:

```csharp
var localizer = factory.Create("Pages/IndexPage", "MyApp.Web");   // base name, assembly
var shared = factory.Create(string.Empty, "MyApp.Web");           // that assembly's shared file
```

## License

MIT.
