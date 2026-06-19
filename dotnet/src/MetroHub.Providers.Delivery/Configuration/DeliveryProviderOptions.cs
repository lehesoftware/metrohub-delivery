namespace MetroHub.Providers.Delivery.Configuration;

/// <summary>Fluent builder passed to <c>AddDeliveryDispatch(options => …)</c>.</summary>
public sealed class DeliveryProviderOptions
{
    internal ProviderKind Kind { get; private set; } = ProviderKind.None;
    internal ShipDayConfig? ShipDayConfig { get; private set; }
    internal FleetbaseConfig? FleetbaseConfig { get; private set; }

    /// <summary>Use ShipDay as the delivery platform.</summary>
    public DeliveryProviderOptions UseShipDay(Action<ShipDayConfig> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        AssertSingleProvider();
        var cfg = new ShipDayConfig();
        configure(cfg);
        cfg.Validate();
        Kind = ProviderKind.ShipDay;
        ShipDayConfig = cfg;
        return this;
    }

    /// <summary>Use a self-hosted Fleetbase FleetOps instance as the delivery platform.</summary>
    public DeliveryProviderOptions UseFleetbase(Action<FleetbaseConfig> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        AssertSingleProvider();
        var cfg = new FleetbaseConfig();
        configure(cfg);
        cfg.Validate();
        Kind = ProviderKind.Fleetbase;
        FleetbaseConfig = cfg;
        return this;
    }

    private void AssertSingleProvider()
    {
        if (Kind != ProviderKind.None)
            throw new InvalidOperationException(
                "Only one delivery provider may be configured. Call UseShipDay() or UseFleetbase(), not both.");
    }
}

internal enum ProviderKind { None, ShipDay, Fleetbase }
