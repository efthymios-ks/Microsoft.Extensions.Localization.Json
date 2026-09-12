using Microsoft.Extensions.Localization.Json.Tests.Widgets;

namespace Microsoft.Extensions.Localization.Json.Tests;

public sealed class ResourceLookupTests
{
    private static readonly string _testAssembly = typeof(ResourceLookupTests).Assembly!.FullName!;

    [Theory]
    [InlineData("which", "specific")]
    [InlineData("specific", "US")]
    [InlineData("parent", "EN")]
    [InlineData("default", "FR")]
    [InlineData("neutral", "NEUTRAL")]
    public void Indexer_WhenWalkingTheChain_ShouldTakeTheFirstFileCarryingTheKey(string name, string expected)
    {
        // Arrange
        using var culture = CultureScope.Use("en-US");
        var localizer = CreateFactory(options => options.DefaultCulture = "fr").Create(typeof(Chain));

        // Act
        var localized = localizer[name];

        // Assert
        Assert.Equal(expected, localized.Value);
    }

    [Fact]
    public void GetAllStrings_WhenWalkingTheChain_ShouldReachEveryStepOfIt()
    {
        // Arrange
        using var culture = CultureScope.Use("en-US");
        var localizer = CreateFactory(options => options.DefaultCulture = "fr").Create(typeof(Chain));

        // Act
        var strings = localizer
            .GetAllStrings(includeParentCultures: true)
            .ToDictionary(entry => entry.Name, entry => entry.Value);

        // Assert
        Assert.Equal(["default", "neutral", "parent", "specific", "which"], strings.Keys.Order());
        Assert.Equal("specific", strings["which"]);
    }

    [Fact]
    public void Indexer_WhenTheCultureIsTheDefaultOne_ShouldNotLookForItTwice()
    {
        // Arrange
        using var culture = CultureScope.Use("fr");
        var localizer = CreateFactory(options => options.DefaultCulture = "fr").Create(typeof(Chain));

        // Act
        var localized = localizer["which"];

        // Assert
        Assert.Equal("default", localized.Value);
    }

    [Fact]
    public void Indexer_WhenNothingMatchesTheCulture_ShouldEndAtTheCultureLessFile()
    {
        // Arrange
        using var culture = CultureScope.Use("de-DE");
        var localizer = CreateFactory().Create(typeof(Chain));

        // Act
        var localized = localizer["which"];

        // Assert
        Assert.Equal("neutral", localized.Value);
    }

    [Fact]
    public void Indexer_WhenAFileSitsBothNextToTheTypeAndUnderTheResourcesPath_ShouldPreferTheOneNextToIt()
    {
        // Arrange
        using var culture = CultureScope.Use("en");
        var localizer = CreateFactory(options => options.ResourcesPath = "Resources").Create(typeof(Placement));

        // Act
        var localized = localizer["where"];

        // Assert
        Assert.Equal("next to the type", localized.Value);
    }

    [Fact]
    public void Indexer_WhenAFileSitsNextToTheType_ShouldIgnoreTheResourcesPathCopyEntirely()
    {
        // Arrange
        using var culture = CultureScope.Use("en");
        var localizer = CreateFactory(options => options.ResourcesPath = "Resources").Create(typeof(Placement));

        // Act
        var localized = localizer["onlyUnderResourcesPath"];

        // Assert
        Assert.True(localized.ResourceNotFound);
    }

    [Fact]
    public void Indexer_WhenTheOnlyFileIsUnderTheResourcesPath_ShouldReadIt()
    {
        // Arrange
        using var culture = CultureScope.Use("el");
        var localizer = CreateFactory(options => options.ResourcesPath = "Resources").Create(typeof(Placement));

        // Act
        var localized = localizer["where"];

        // Assert
        Assert.Equal("under the resources path", localized.Value);
    }

    [Fact]
    public void Indexer_WhenTheResourcesPathIsElsewhere_ShouldStopLookingUnderTheOldOne()
    {
        // Arrange
        using var culture = CultureScope.Use("en");
        var localizer = CreateFactory(options => options.ResourcesPath = "Texts").Create(typeof(Placement));

        // Act
        var localized = localizer["onlyUnderResourcesPath"];

        // Assert
        Assert.True(localized.ResourceNotFound);
    }

    [Fact]
    public void Create_WhenTheBaseNameIsEmpty_ShouldReadTheAssemblysSharedFile()
    {
        // Arrange
        using var culture = CultureScope.Use("el");
        var localizer = CreateFactory().Create(string.Empty, _testAssembly);

        // Act
        var localized = localizer["hello"];

        // Assert
        Assert.Equal("Γεια", localized.Value);
    }

    [Fact]
    public void Create_WhenTheSharedFileHasNoCultureMatch_ShouldEndAtTheCultureLessSharedFile()
    {
        // Arrange
        using var culture = CultureScope.Use("de-DE");
        var localizer = CreateFactory().Create(string.Empty, _testAssembly);

        // Act
        var localized = localizer["neutralShared"];

        // Assert
        Assert.Equal("Shared neutral", localized.Value);
    }

    [Fact]
    public void Create_WhenTheSharedFileIsAskedForWithAnotherResourcesPath_ShouldLookThere()
    {
        // Arrange
        using var culture = CultureScope.Use("en");
        var localizer = CreateFactory(options => options.ResourcesPath = "Texts").Create(string.Empty, _testAssembly);

        // Act
        var localized = localizer["hello"];

        // Assert
        Assert.True(localized.ResourceNotFound);
    }

    private static IStringLocalizerFactory CreateFactory(Action<JsonLocalizationOptions>? configure = null)
        => JsonStringLocalizerTests.CreateFactory(configure);
}
