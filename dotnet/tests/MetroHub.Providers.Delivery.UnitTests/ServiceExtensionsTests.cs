using MetroHub.Providers.Delivery.Extensions;
using MetroHub.Providers.Delivery.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace MetroHub.Providers.Delivery.UnitTests;

public sealed class ServiceExtensionsTests
{
    [Fact]
    public void UseShipDay_ResolvesIDeliveryDispatch()
    {
        var sp = Build(o => o.UseShipDay(cfg => cfg.ApiKey = "test-key"));
        using var scope = sp.CreateScope();
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<IDeliveryDispatch>());
    }

    [Fact]
    public void UseFleetbase_ResolvesIDeliveryDispatch()
    {
        var sp = Build(o => o.UseFleetbase(cfg =>
        {
            cfg.ApiKey  = "test-key";
            cfg.BaseUrl = "https://fleet.example.com/api";
        }));
        using var scope = sp.CreateScope();
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<IDeliveryDispatch>());
    }

    [Fact]
    public void NoProvider_Throws()
    {
        Assert.Throws<InvalidOperationException>(() =>
            Build(_ => { }));
    }

    [Fact]
    public void BothProviders_Throws()
    {
        Assert.Throws<InvalidOperationException>(() =>
            Build(o =>
            {
                o.UseShipDay(cfg => cfg.ApiKey = "k");
                o.UseFleetbase(cfg => { cfg.ApiKey = "k"; cfg.BaseUrl = "https://x.com"; });
            }));
    }

    [Fact]
    public void ShipDayConfig_EmptyApiKey_Throws()
    {
        Assert.Throws<InvalidOperationException>(() =>
            Build(o => o.UseShipDay(cfg => cfg.ApiKey = "")));
    }

    [Fact]
    public void FleetbaseConfig_EmptyBaseUrl_Throws()
    {
        Assert.Throws<InvalidOperationException>(() =>
            Build(o => o.UseFleetbase(cfg =>
            {
                cfg.ApiKey  = "k";
                cfg.BaseUrl = "";
            })));
    }

    [Fact]
    public void FleetbaseConfig_EmptyApiKey_Throws()
    {
        Assert.Throws<InvalidOperationException>(() =>
            Build(o => o.UseFleetbase(cfg =>
            {
                cfg.ApiKey  = "";
                cfg.BaseUrl = "https://x.com";
            })));
    }

    private static ServiceProvider Build(Action<Configuration.DeliveryProviderOptions> configure)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDeliveryDispatch(configure);
        return services.BuildServiceProvider();
    }
}
