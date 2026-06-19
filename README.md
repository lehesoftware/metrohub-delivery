# metrohub-delivery

Plug-and-play delivery dispatch provider for MetroHub. The application layer talks only to `IDeliveryDispatch`; switching between ShipDay, Fleetbase, or any future provider is a configuration change with no application code edits.

## Providers

| Provider | Status | Auth |
|----------|--------|------|
| ShipDay | ✓ Supported | Basic (API key) |
| Fleetbase (self-hosted) | ✓ Supported | Bearer (API key + base URL) |

## Installation

```xml
<PackageReference Include="MetroHub.Providers.Delivery" Version="1.0.0" />
```

## Quick start

```csharp
// ShipDay
services.AddDeliveryDispatch(options => options.UseShipDay(cfg =>
{
    cfg.ApiKey = "your-shipday-key";
}));

// Fleetbase (self-hosted)
services.AddDeliveryDispatch(options => options.UseFleetbase(cfg =>
{
    cfg.ApiKey  = "your-fleetbase-bearer-token";
    cfg.BaseUrl = "https://your-fleetbase-host/api";
}));
```

The registered `IDeliveryDispatch` is scoped. Inject it wherever you push delivery orders:

```csharp
public class OrderService(IDeliveryDispatch delivery)
{
    public async Task PushDriverNoteAsync(DeliveryOrder order, CancellationToken ct)
    {
        await delivery.UpsertOrderAsync(order, ct);
    }
}
```

## License

MIT
