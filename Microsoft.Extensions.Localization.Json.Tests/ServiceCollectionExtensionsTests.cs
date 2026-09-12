using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization.Json.Tests.Widgets;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Localization.Json.Tests;

public sealed class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddJsonLocalization_WhenCalled_ShouldResolveTheFactoryAndBothLocalizers()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddJsonLocalization();

        // Assert
        using var provider = services.BuildServiceProvider();

        Assert.IsType<JsonStringLocalizerFactory>(provider.GetService<IStringLocalizerFactory>());
        Assert.NotNull(provider.GetService<IStringLocalizer>());
        Assert.NotNull(provider.GetService<IStringLocalizer<Widget>>());
    }

    [Fact]
    public void AddJsonLocalization_WhenTheGenericLocalizerIsUsed_ShouldReadItsTypesFile()
    {
        // Arrange
        using var culture = CultureScope.Use("en");
        var services = new ServiceCollection();

        // Act
        services.AddJsonLocalization();

        // Assert
        using var provider = services.BuildServiceProvider();

        Assert.Equal("Widget", provider.GetRequiredService<IStringLocalizer<Widget>>()["title"]);
    }

    [Fact]
    public void AddJsonLocalization_WhenConfigured_ShouldApplyTheOptions()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddJsonLocalization(options =>
        {
            options.ResourcesPath = "Texts";
            options.DefaultCulture = "en";
        });

        // Assert
        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<JsonLocalizationOptions>>().Value;

        Assert.Equal("Texts", options.ResourcesPath);
        Assert.Equal("en", options.DefaultCulture);
    }

    [Fact]
    public void AddJsonLocalization_WhenNotConfigured_ShouldKeepTheDefaults()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddJsonLocalization();

        // Assert
        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<JsonLocalizationOptions>>().Value;

        Assert.Equal("Resources", options.ResourcesPath);
        Assert.Null(options.DefaultCulture);
    }

    [Fact]
    public void AddJsonLocalization_WhenAFactoryIsAlreadyRegistered_ShouldLeaveItAlone()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<IStringLocalizerFactory, AlreadyRegisteredFactory>();

        // Act
        services.AddJsonLocalization();

        // Assert
        using var provider = services.BuildServiceProvider();

        Assert.IsType<AlreadyRegisteredFactory>(provider.GetService<IStringLocalizerFactory>());
    }

    [Fact]
    public void AddJsonLocalization_WhenCalledTwice_ShouldRegisterOneOfEach()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services
            .AddJsonLocalization()
            .AddJsonLocalization();

        // Assert
        Assert.Single(services, service => service.ServiceType == typeof(IStringLocalizerFactory));
        Assert.Single(services, service => service.ServiceType == typeof(IStringLocalizer));
    }

    [Fact]
    public void AddJsonLocalization_WhenTheServicesAreNull_ShouldThrow()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => ((IServiceCollection)null!).AddJsonLocalization());
    }

    private sealed class AlreadyRegisteredFactory : IStringLocalizerFactory
    {
        public IStringLocalizer Create(Type resourceSource)
            => throw new NotSupportedException();

        public IStringLocalizer Create(string baseName, string location)
            => throw new NotSupportedException();
    }
}
