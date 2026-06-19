using MetroHub.Providers.Delivery.Configuration;
using MetroHub.Providers.Delivery.Interfaces;
using MetroHub.Providers.Delivery.Providers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MetroHub.Providers.Delivery.Extensions;

/// <summary>Registers the delivery dispatch provider with the host's DI container.</summary>
public static class ServiceExtensions
{
    /// <summary>
    /// Registers <see cref="IDeliveryDispatch"/> backed by the chosen provider.
    /// Call exactly one of <c>UseShipDay</c> or <c>UseFleetbase</c> inside <paramref name="configure"/>.
    /// </summary>
    /// <example>
    /// // ShipDay
    /// services.AddDeliveryDispatch(o => o.UseShipDay(cfg => cfg.ApiKey = "…"));
    ///
    /// // Fleetbase (self-hosted)
    /// services.AddDeliveryDispatch(o => o.UseFleetbase(cfg =>
    /// {
    ///     cfg.ApiKey  = "…";
    ///     cfg.BaseUrl = "https://fleet.example.com/api";
    /// }));
    /// </example>
    public static IServiceCollection AddDeliveryDispatch(
        this IServiceCollection services,
        Action<DeliveryProviderOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        var options = new DeliveryProviderOptions();
        configure(options);

        return options.Kind switch
        {
            ProviderKind.ShipDay   => RegisterShipDay(services, options.ShipDayConfig!),
            ProviderKind.Fleetbase => RegisterFleetbase(services, options.FleetbaseConfig!),
            _ => throw new InvalidOperationException(
                "No delivery provider selected. Call UseShipDay() or UseFleetbase() inside AddDeliveryDispatch.")
        };
    }

    /// <summary>
    /// Registers <see cref="IDeliveryDispatch"/> driven by <c>IConfiguration</c>.
    /// Reads the <c>Delivery</c> section; the required key is <c>Delivery__Provider</c>
    /// (case-insensitive: <c>ShipDay</c> or <c>Fleetbase</c>).
    /// <list type="bullet">
    ///   <item><c>Delivery__Provider=ShipDay</c> → also reads <c>Delivery__ApiKey</c></item>
    ///   <item><c>Delivery__Provider=Fleetbase</c> → also reads <c>Delivery__ApiKey</c> + <c>Delivery__BaseUrl</c></item>
    /// </list>
    /// When <c>Delivery__Provider</c> is absent or empty the registration is skipped and
    /// <see cref="IDeliveryDispatch"/> is NOT registered — callers that require it will fail
    /// at DI resolution time with a clear "not registered" message.
    /// </summary>
    public static IServiceCollection AddDeliveryDispatch(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var section = configuration.GetSection("Delivery");
        var provider = section["Provider"];

        if (string.IsNullOrWhiteSpace(provider))
            return services; // fail-soft: no provider configured, dispatch stays unregistered

        return provider.Trim().ToUpperInvariant() switch
        {
            "SHIPDAY" => services.AddDeliveryDispatch(o => o.UseShipDay(cfg =>
            {
                cfg.ApiKey = section["ApiKey"] ?? string.Empty;
            })),
            "FLEETBASE" => services.AddDeliveryDispatch(o => o.UseFleetbase(cfg =>
            {
                cfg.ApiKey  = section["ApiKey"]  ?? string.Empty;
                cfg.BaseUrl = section["BaseUrl"] ?? string.Empty;
            })),
            _ => throw new InvalidOperationException(
                $"Unknown Delivery:Provider value '{provider}'. Expected 'ShipDay' or 'Fleetbase'.")
        };
    }

    private static IServiceCollection RegisterShipDay(IServiceCollection services, ShipDayConfig config)
    {
        services.AddSingleton(config);
        services.AddHttpClient(ShipDayDeliveryProvider.HttpClientName,
            c => c.Timeout = TimeSpan.FromSeconds(15));
        services.AddScoped<IDeliveryDispatch, ShipDayDeliveryProvider>();
        return services;
    }

    private static IServiceCollection RegisterFleetbase(IServiceCollection services, FleetbaseConfig config)
    {
        services.AddSingleton(config);
        services.AddHttpClient(FleetbaseDeliveryProvider.HttpClientName,
            c => c.Timeout = TimeSpan.FromSeconds(15));
        services.AddScoped<IDeliveryDispatch, FleetbaseDeliveryProvider>();
        return services;
    }
}
