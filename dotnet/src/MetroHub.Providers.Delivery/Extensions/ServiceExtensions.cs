using MetroHub.Providers.Delivery.Configuration;
using MetroHub.Providers.Delivery.Interfaces;
using MetroHub.Providers.Delivery.Providers;
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
    /// // Fleetbase
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
