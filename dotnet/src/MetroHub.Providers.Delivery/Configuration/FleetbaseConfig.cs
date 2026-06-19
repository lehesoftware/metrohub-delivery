namespace MetroHub.Providers.Delivery.Configuration;

/// <summary>Fleetbase FleetOps provider configuration.</summary>
public sealed class FleetbaseConfig
{
    /// <summary>
    /// Bearer token from the Fleetbase console
    /// (<c>Authorization: Bearer {ApiKey}</c>).
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Base URL of the self-hosted Fleetbase instance,
    /// e.g. <c>https://fleet.example.com/api</c>. Trailing slash is stripped automatically.
    /// </summary>
    public string BaseUrl { get; set; } = string.Empty;

    internal void Validate()
    {
        if (string.IsNullOrWhiteSpace(ApiKey))
            throw new InvalidOperationException("FleetbaseConfig.ApiKey must be set.");
        if (string.IsNullOrWhiteSpace(BaseUrl))
            throw new InvalidOperationException("FleetbaseConfig.BaseUrl must be set.");
    }
}
