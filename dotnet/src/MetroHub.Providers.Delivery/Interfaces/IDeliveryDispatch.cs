using MetroHub.Providers.Delivery.Models;

namespace MetroHub.Providers.Delivery.Interfaces;

/// <summary>
/// Platform-neutral delivery dispatch contract. Implementations push orders to a
/// delivery platform (ShipDay, Fleetbase, …) and keep them in sync as driver notes
/// are added or updated. The concrete provider is chosen at startup via
/// <c>AddDeliveryDispatch</c> in <see cref="Extensions.ServiceExtensions"/>.
/// </summary>
public interface IDeliveryDispatch
{
    /// <summary>
    /// Looks up the order on the delivery platform by order number.
    /// Returns a platform-neutral string ID when found, or <c>null</c> when the
    /// platform has no record of the order yet.
    /// </summary>
    Task<string?> FindOrderAsync(string orderNumber, CancellationToken cancellationToken = default);

    /// <summary>
    /// Upserts the order on the delivery platform: updates the existing record when
    /// the platform already knows the order, inserts a new one otherwise.
    /// Throws <see cref="DeliveryApiException"/> on any platform error.
    /// </summary>
    Task UpsertOrderAsync(DeliveryOrder order, CancellationToken cancellationToken = default);
}
