using System.Text.Json;
using System.Text.Json.Serialization;
using MetroHub.Providers.Delivery.Configuration;
using MetroHub.Providers.Delivery.Interfaces;
using MetroHub.Providers.Delivery.Models;
using Microsoft.Extensions.Logging;

namespace MetroHub.Providers.Delivery.Providers;

/// <summary>
/// <see cref="IDeliveryDispatch"/> backed by ShipDay (<c>https://api.shipday.com</c>).
/// Authenticates with <c>Authorization: Basic {ApiKey}</c> (the key IS the credential —
/// not a base64 user:pass pair). JSON is camelCase; nulls are omitted.
/// </summary>
internal sealed class ShipDayDeliveryProvider : IDeliveryDispatch
{
    internal const string HttpClientName = "MetroHub.Providers.Delivery.ShipDay";
    private const string BaseUrl = "https://api.shipday.com";
    private const int MaxErrorBodyLength = 300;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private readonly IHttpClientFactory _httpFactory;
    private readonly ShipDayConfig _config;
    private readonly ILogger<ShipDayDeliveryProvider> _logger;

    public ShipDayDeliveryProvider(
        IHttpClientFactory httpFactory,
        ShipDayConfig config,
        ILogger<ShipDayDeliveryProvider> logger)
    {
        _httpFactory = httpFactory;
        _config = config;
        _logger = logger;
    }

    public async Task<string?> FindOrderAsync(string orderNumber, CancellationToken cancellationToken = default)
    {
        using var response = await SendAsync(
            HttpMethod.Get,
            $"/orders/{Uri.EscapeDataString(orderNumber)}",
            body: null,
            cancellationToken);

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
        await EnsureSuccessAsync(response, "look up order", cancellationToken);

        var raw = await response.Content.ReadAsStringAsync(cancellationToken);
        try
        {
            using var doc = JsonDocument.Parse(raw);
            if (doc.RootElement.ValueKind != JsonValueKind.Array) return null;
            foreach (var order in doc.RootElement.EnumerateArray())
            {
                if (order.TryGetProperty("orderId", out var id) && id.TryGetInt64(out var orderId))
                    return orderId.ToString();
            }
            return null;
        }
        catch (JsonException ex)
        {
            throw new DeliveryApiException("ShipDay returned an unreadable order-lookup response.", ex);
        }
    }

    public async Task UpsertOrderAsync(DeliveryOrder order, CancellationToken cancellationToken = default)
    {
        var existingId = await FindOrderAsync(order.OrderNumber, cancellationToken);
        var payload = ToPayload(order);

        if (existingId is not null)
        {
            using var updateResp = await SendAsync(
                HttpMethod.Put, $"/order/edit/{existingId}", payload, cancellationToken);
            await EnsureSuccessAsync(updateResp, "update order", cancellationToken);
            return;
        }

        using var insertResp = await SendAsync(HttpMethod.Post, "/orders", payload, cancellationToken);
        await EnsureSuccessAsync(insertResp, "create order", cancellationToken);

        // ShipDay can report failure in a 200 body with { "success": false }
        var body = await insertResp.Content.ReadAsStringAsync(cancellationToken);
        try
        {
            using var doc = JsonDocument.Parse(body);
            if (doc.RootElement.ValueKind == JsonValueKind.Object
                && doc.RootElement.TryGetProperty("success", out var success)
                && success.ValueKind == JsonValueKind.False)
            {
                throw new DeliveryApiException($"ShipDay rejected the order create: {Truncate(body)}");
            }
        }
        catch (JsonException) { /* 2xx non-JSON body = accepted */ }
    }

    private static ShipDayPayload ToPayload(DeliveryOrder order) => new()
    {
        OrderNumber = order.OrderNumber,
        CustomerName = order.CustomerName,
        CustomerAddress = order.CustomerAddress,
        CustomerEmail = order.CustomerEmail,
        CustomerPhoneNumber = order.CustomerPhone,
        RestaurantName = order.PickupName,
        RestaurantAddress = order.PickupAddress,
        RestaurantPhoneNumber = order.PickupPhone,
        DeliveryInstruction = order.DriverInstruction,
        TotalOrderCost = order.TotalCost,
        OrderItem = order.LineItems?.Select(i => new ShipDayOrderItem
        {
            Name = i.Name,
            Quantity = i.Quantity,
            UnitPrice = i.UnitPrice,
        }).ToList(),
    };

    private async Task<HttpResponseMessage> SendAsync(
        HttpMethod method, string path, object? body, CancellationToken ct)
    {
        var client = _httpFactory.CreateClient(HttpClientName);
        using var request = new HttpRequestMessage(method, BaseUrl + path);
        request.Headers.TryAddWithoutValidation("Authorization", $"Basic {_config.ApiKey.Trim()}");
        if (body is not null)
        {
            request.Content = new StringContent(
                JsonSerializer.Serialize(body, JsonOptions),
                System.Text.Encoding.UTF8,
                "application/json");
        }
        try
        {
            return await client.SendAsync(request, ct);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            throw new DeliveryApiException("ShipDay did not respond in time (request timed out).");
        }
        catch (HttpRequestException ex)
        {
            throw new DeliveryApiException($"ShipDay request failed: {ex.Message}", ex);
        }
    }

    private async Task EnsureSuccessAsync(HttpResponseMessage response, string action, CancellationToken ct)
    {
        if (response.IsSuccessStatusCode) return;
        var body = await response.Content.ReadAsStringAsync(ct);
        _logger.LogWarning("ShipDay {Action} failed with HTTP {Status}.", action, (int)response.StatusCode);
        throw new DeliveryApiException(
            $"ShipDay could not {action} (HTTP {(int)response.StatusCode}): {Truncate(body)}");
    }

    private static string Truncate(string value)
    {
        var t = value.Trim();
        return t.Length <= MaxErrorBodyLength ? t : t[..MaxErrorBodyLength] + "…";
    }
}

// Internal payload records — serialized via camelCase JsonOptions above.
internal sealed record ShipDayPayload
{
    public required string OrderNumber { get; init; }
    public string? CustomerName { get; init; }
    public string? CustomerAddress { get; init; }
    public string? CustomerEmail { get; init; }
    public string? CustomerPhoneNumber { get; init; }
    public string? RestaurantName { get; init; }
    public string? RestaurantAddress { get; init; }
    public string? RestaurantPhoneNumber { get; init; }
    public string? DeliveryInstruction { get; init; }
    public decimal? TotalOrderCost { get; init; }
    public IReadOnlyList<ShipDayOrderItem>? OrderItem { get; init; }
}

internal sealed record ShipDayOrderItem
{
    public required string Name { get; init; }
    public required int Quantity { get; init; }
    public decimal? UnitPrice { get; init; }
}
