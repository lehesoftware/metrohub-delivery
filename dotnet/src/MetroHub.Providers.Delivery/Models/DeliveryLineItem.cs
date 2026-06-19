namespace MetroHub.Providers.Delivery.Models;

/// <summary>One line item shown to the driver on the delivery order.</summary>
public sealed record DeliveryLineItem
{
    public required string Name { get; init; }
    public required int Quantity { get; init; }
    public decimal? UnitPrice { get; init; }
}
