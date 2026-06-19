namespace MetroHub.Providers.Delivery.Models;

/// <summary>
/// Platform-neutral delivery order passed to <see cref="Interfaces.IDeliveryDispatch"/>.
/// Each provider maps this to its own wire format — ShipDay uses camelCase flat JSON;
/// Fleetbase uses snake_case nested JSON. Nulls are omitted by all providers.
/// </summary>
public sealed record DeliveryOrder
{
    /// <summary>External order number (e.g. Shopify order number without the leading #).</summary>
    public required string OrderNumber { get; init; }

    public string? CustomerName { get; init; }
    public string? CustomerAddress { get; init; }
    public string? CustomerEmail { get; init; }
    public string? CustomerPhone { get; init; }

    /// <summary>Pickup point name (store / restaurant) shown to the driver.</summary>
    public string? PickupName { get; init; }
    public string? PickupAddress { get; init; }
    public string? PickupPhone { get; init; }

    /// <summary>Free-text instruction composed from all for-driver notes on the order.</summary>
    public string? DriverInstruction { get; init; }

    public decimal? TotalCost { get; init; }

    public IReadOnlyList<DeliveryLineItem>? LineItems { get; init; }
}
