using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization.Json.Tests.Widgets;

namespace Microsoft.Extensions.Localization.Json.Tests;

public sealed class JsonStringLocalizerTests
{
    private static readonly string _testAssembly = typeof(JsonStringLocalizerTests).Assembly!.FullName!;

    [Fact]
    public void Indexer_WhenTheKeyExists_ShouldReturnItsValue()
    {
        // Arrange
        using var culture = CultureScope.Use("en");
        var localizer = CreateSharedLocalizer();

        // Act
        var localized = localizer["hello"];

        // Assert
        Assert.Equal("Hello", localized.Value);
        Assert.False(localized.ResourceNotFound);
    }

    [Fact]
    public void Indexer_WhenTheKeyIsMissing_ShouldReturnTheNameAndSaySo()
    {
        // Arrange
        using var culture = CultureScope.Use("en");
        var localizer = CreateSharedLocalizer();

        // Act
        var localized = localizer["nothing.here"];

        // Assert
        Assert.Equal("nothing.here", localized.Value);
        Assert.True(localized.ResourceNotFound);
        Assert.Equal("Microsoft.Extensions.Localization.Json.Tests", localized.SearchedLocation);
    }

    [Fact]
    public void Indexer_WhenArgumentsAreGiven_ShouldFormatTheValue()
    {
        // Arrange
        using var culture = CultureScope.Use("en");
        var localizer = CreateSharedLocalizer();

        // Act
        var localized = localizer["greet", "Ann"];

        // Assert
        Assert.Equal("Hello Ann", localized.Value);
    }

    [Fact]
    public void Indexer_WhenTheKeyIsMissingAndArgumentsAreGiven_ShouldReturnTheName()
    {
        // Arrange
        using var culture = CultureScope.Use("en");
        var localizer = CreateSharedLocalizer();

        // Act
        var localized = localizer["nothing.here", "Ann"];

        // Assert
        Assert.Equal("nothing.here", localized.Value);
        Assert.True(localized.ResourceNotFound);
    }

    [Fact]
    public void Indexer_WhenTheCultureIsSpecific_ShouldPreferItOverItsParent()
    {
        // Arrange
        using var culture = CultureScope.Use("el-GR");
        var localizer = CreateSharedLocalizer();

        // Act
        var localized = localizer["hello"];

        // Assert
        Assert.Equal("Γεια σου", localized.Value);
    }

    [Fact]
    public void Indexer_WhenTheSpecificFileMissesTheKey_ShouldFallBackToTheParentCulture()
    {
        // Arrange
        using var culture = CultureScope.Use("el-GR");
        var localizer = CreateSharedLocalizer();

        // Act
        var localized = localizer["form.title"];

        // Assert
        Assert.Equal("Φόρμα", localized.Value);
    }

    [Fact]
    public void Indexer_WhenNothingInTheChainHasIt_ShouldFallBackToTheDefaultCulture()
    {
        // Arrange
        using var culture = CultureScope.Use("fr-FR");
        var localizer = CreateSharedLocalizer(options => options.DefaultCulture = "en");

        // Act
        var localized = localizer["hello"];

        // Assert
        Assert.Equal("Hello", localized.Value);
    }

    [Fact]
    public void Indexer_WhenNoDefaultCultureIsConfigured_ShouldNotFallBackToOne()
    {
        // Arrange
        using var culture = CultureScope.Use("fr-FR");
        var localizer = CreateSharedLocalizer();

        // Act
        var localized = localizer["hello"];

        // Assert
        Assert.True(localized.ResourceNotFound);
    }

    [Fact]
    public void Indexer_WhenTheValueIsNested_ShouldReadItAsADottedKey()
    {
        // Arrange
        using var culture = CultureScope.Use("en");
        var localizer = CreateSharedLocalizer();

        // Act
        var localized = localizer["form.title"];

        // Assert
        Assert.Equal("Form", localized.Value);
    }

    [Fact]
    public void Indexer_WhenTheValueIsAnArray_ShouldNumberItsEntries()
    {
        // Arrange
        using var culture = CultureScope.Use("en");
        var localizer = CreateSharedLocalizer();

        // Act & Assert
        Assert.Equal("First", localizer["form.fields.0"].Value);
        Assert.Equal("Second", localizer["form.fields.1"].Value);
    }

    [Theory]
    [InlineData("count", "3")]
    [InlineData("enabled", "true")]
    public void Indexer_WhenTheValueIsNotAString_ShouldReadItAsWritten(string name, string expected)
    {
        // Arrange
        using var culture = CultureScope.Use("en");
        var localizer = CreateSharedLocalizer();

        // Act
        var localized = localizer[name];

        // Assert
        Assert.Equal(expected, localized.Value);
    }

    [Fact]
    public void Indexer_WhenTheValueIsNull_ShouldCountAsMissing()
    {
        // Arrange
        using var culture = CultureScope.Use("en");
        var localizer = CreateSharedLocalizer();

        // Act
        var localized = localizer["missingValue"];

        // Assert
        Assert.True(localized.ResourceNotFound);
    }

    [Fact]
    public void Indexer_WhenTheAssemblyHasNoFileForTheResource_ShouldReturnTheName()
    {
        // Arrange
        using var culture = CultureScope.Use("en");
        var localizer = CreateFactory().Create("Nowhere.Missing", _testAssembly);

        // Act
        var localized = localizer["hello"];

        // Assert
        Assert.True(localized.ResourceNotFound);
    }

    [Fact]
    public void Indexer_WhenTheNameIsEmpty_ShouldThrow()
    {
        // Arrange
        var localizer = CreateSharedLocalizer();

        // Act & Assert
        Assert.ThrowsAny<ArgumentException>(() => localizer[string.Empty]);
    }

    [Fact]
    public void Indexer_WhenTheArgumentsAreNull_ShouldThrow()
    {
        // Arrange
        var localizer = CreateSharedLocalizer();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => localizer["greet", null!]);
    }

    [Fact]
    public void Indexer_WhenTheFileIsNotValidJson_ShouldThrow()
    {
        // Arrange
        using var culture = CultureScope.Use("el");
        var localizer = CreateFactory().Create(typeof(Broken));

        // Act & Assert
        Assert.ThrowsAny<JsonException>(() => localizer["title"]);
    }

    [Fact]
    public void GetAllStrings_WhenParentsAreIncluded_ShouldMergeTheChainWithTheClosestWinning()
    {
        // Arrange
        using var culture = CultureScope.Use("el-GR");
        var localizer = CreateSharedLocalizer(options => options.DefaultCulture = "en");

        // Act
        var strings = localizer
            .GetAllStrings(includeParentCultures: true)
            .ToDictionary(entry => entry.Name, entry => entry.Value);

        // Assert
        Assert.Equal("Γεια σου", strings["hello"]);
        Assert.Equal("Φόρμα", strings["form.title"]);
        Assert.Equal("Shared english", strings["shared"]);
    }

    [Fact]
    public void GetAllStrings_WhenParentsAreExcluded_ShouldOnlyReturnThatCulture()
    {
        // Arrange
        using var culture = CultureScope.Use("el-GR");
        var localizer = CreateSharedLocalizer(options => options.DefaultCulture = "en");

        // Act
        var strings = localizer.GetAllStrings(includeParentCultures: false).ToArray();

        // Assert
        Assert.Equal(["hello"], strings.Select(entry => entry.Name));
    }

    [Fact]
    public void GetAllStrings_WhenThereIsNoFile_ShouldBeEmpty()
    {
        // Arrange
        using var culture = CultureScope.Use("en");
        var localizer = CreateFactory().Create("Nowhere.Missing", _testAssembly);

        // Act
        var strings = localizer.GetAllStrings(includeParentCultures: true);

        // Assert
        Assert.Empty(strings);
    }

    internal static IStringLocalizerFactory CreateFactory(Action<JsonLocalizationOptions>? configure = null)
        => new ServiceCollection()
            .AddJsonLocalization(configure)
            .BuildServiceProvider()
            .GetRequiredService<IStringLocalizerFactory>();

    private static IStringLocalizer CreateSharedLocalizer(Action<JsonLocalizationOptions>? configure = null)
        => CreateFactory(configure).Create(string.Empty, _testAssembly);
}
