namespace MetroHub.Providers.Delivery.Interfaces;

/// <summary>
/// A non-success response from a delivery platform API. The message is safe to
/// persist as a sync-error and show to admins (HTTP status + a bounded slice of
/// the response body; never an API key).
/// </summary>
public sealed class DeliveryApiException : Exception
{
    public DeliveryApiException(string message)
        : base(message) { }

    public DeliveryApiException(string message, Exception innerException)
        : base(message, innerException) { }
}
