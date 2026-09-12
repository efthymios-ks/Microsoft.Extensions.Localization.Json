using Microsoft.Extensions.Localization.Json.Tests.Widgets;

namespace Microsoft.Extensions.Localization.Json.Tests;

public sealed class JsonStringLocalizerFactoryTests
{
    private static readonly string _testAssembly = typeof(JsonStringLocalizerFactoryTests).Assembly!.FullName!;

    [Fact]
    public void Create_WhenGivenAType_ShouldReadTheFileNamedAfterIt()
    {
        // Arrange
        using var culture = CultureScope.Use("en");
        var localizer = JsonStringLocalizerTests.CreateFactory().Create(typeof(Widget));

        // Act
        var localized = localizer["title"];

        // Assert
        Assert.Equal("Widget", localized.Value);
    }

    [Fact]
    public void Create_WhenTheCultureFileMissesTheKey_ShouldFallBackToTheCultureLessFile()
    {
        // Arrange
        using var culture = CultureScope.Use("en");
        var localizer = JsonStringLocalizerTests.CreateFactory().Create(typeof(Widget));

        // Act
        var localized = localizer["onlyNeutral"];

        // Assert
        Assert.Equal("Neutral only", localized.Value);
    }

    [Fact]
    public void Create_WhenNoCultureFileMatches_ShouldReadTheCultureLessFile()
    {
        // Arrange
        using var culture = CultureScope.Use("fr-FR");
        var localizer = JsonStringLocalizerTests.CreateFactory().Create(typeof(Widget));

        // Act
        var localized = localizer["title"];

        // Assert
        Assert.Equal("Widget (neutral)", localized.Value);
    }

    [Fact]
    public void Create_WhenGivenABaseName_ShouldReadThatFile()
    {
        // Arrange
        using var culture = CultureScope.Use("en");
        var localizer = JsonStringLocalizerTests.CreateFactory().Create("Widgets/Widget", _testAssembly);

        // Act
        var localized = localizer["title"];

        // Assert
        Assert.Equal("Widget", localized.Value);
    }

    [Fact]
    public void Create_WhenGivenADottedBaseName_ShouldReadTheSameFile()
    {
        // Arrange
        using var culture = CultureScope.Use("en");
        var localizer = JsonStringLocalizerTests.CreateFactory().Create("Widgets.Widget", _testAssembly);

        // Act
        var localized = localizer["title"];

        // Assert
        Assert.Equal("Widget", localized.Value);
    }

    [Fact]
    public void Create_WhenTheSameResourceIsAskedForTwice_ShouldHandBackTheSameLocalizer()
    {
        // Arrange
        var factory = JsonStringLocalizerTests.CreateFactory();

        // Act
        var first = factory.Create(typeof(Widget));
        var second = factory.Create(typeof(Widget));

        // Assert
        Assert.Same(first, second);
    }

    [Fact]
    public void Create_WhenTheResourcesPathIsChanged_ShouldLookUnderIt()
    {
        // Arrange
        using var culture = CultureScope.Use("en");
        var factory = JsonStringLocalizerTests.CreateFactory(options => options.ResourcesPath = "Widgets");

        // Act
        var localized = factory.Create("Widget", _testAssembly)["title"];

        // Assert
        Assert.Equal("Widget", localized.Value);
    }

    [Fact]
    public void Create_WhenTheResourcesPathIsEmpty_ShouldStillFindTheFileNextToTheType()
    {
        // Arrange
        using var culture = CultureScope.Use("en");
        var factory = JsonStringLocalizerTests.CreateFactory(options => options.ResourcesPath = string.Empty);

        // Act
        var localized = factory.Create(typeof(Widget))["title"];

        // Assert
        Assert.Equal("Widget", localized.Value);
    }

    [Fact]
    public void Create_WhenTheTypeIsNull_ShouldThrow()
    {
        // Arrange
        var factory = JsonStringLocalizerTests.CreateFactory();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => factory.Create(null!));
    }

    [Fact]
    public void Create_WhenTheAssemblyIsUnknown_ShouldThrow()
    {
        // Arrange
        var factory = JsonStringLocalizerTests.CreateFactory();

        // Act & Assert
        Assert.ThrowsAny<Exception>(() => factory.Create("Widget", "No.Such.Assembly"));
    }

    [Fact]
    public void Create_WhenTheLocationIsEmpty_ShouldReadTheEntryAssembly()
    {
        // Arrange
        using var culture = CultureScope.Use("en");
        var factory = JsonStringLocalizerTests.CreateFactory();

        // Act
        var localized = factory.Create(string.Empty, string.Empty)["hello"];

        // Assert
        Assert.True(localized.ResourceNotFound);
    }
}
