namespace MetroHub.Providers.Delivery.Configuration;

/// <summary>ShipDay provider configuration.</summary>
public sealed class ShipDayConfig
{
    /// <summary>
    /// ShipDay API key — used verbatim as the Basic auth credential
    /// (<c>Authorization: Basic {ApiKey}</c>).
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    internal void Validate()
    {
        if (string.IsNullOrWhiteSpace(ApiKey))
            throw new InvalidOperationException("ShipDayConfig.ApiKey must be set.");
    }
}
