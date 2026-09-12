using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.Localization.Json;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// The plain <see cref="IStringLocalizer"/> reads the entry assembly's shared file; the generic
    /// one reads the file named after its type.
    /// </summary>
    public static IServiceCollection AddJsonLocalization(
        this IServiceCollection services,
        Action<JsonLocalizationOptions>? configure = null
    )
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddOptions();

        if (configure is not null)
        {
            services.Configure(configure);
        }

        services.TryAddSingleton<IStringLocalizerFactory, JsonStringLocalizerFactory>();
        services.TryAddTransient(typeof(IStringLocalizer<>), typeof(StringLocalizer<>));

        services.TryAddSingleton(serviceProvider => serviceProvider
            .GetRequiredService<IStringLocalizerFactory>()
            .Create(string.Empty, string.Empty));

        return services;
    }
}
