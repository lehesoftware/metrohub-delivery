using System.Text.Json;
using System.Text.Json.Serialization;
using MetroHub.Providers.Delivery.Configuration;
using MetroHub.Providers.Delivery.Interfaces;
using MetroHub.Providers.Delivery.Models;
using Microsoft.Extensions.Logging;

namespace MetroHub.Providers.Delivery.Providers;

/// <summary>
/// <see cref="IDeliveryDispatch"/> backed by a self-hosted Fleetbase FleetOps instance.
/// Authenticates with <c>Authorization: Bearer {ApiKey}</c>.
/// JSON is snake_case (<see cref="JsonNamingPolicy.SnakeCaseLower"/>); nulls are omitted.
/// </summary>
internal sealed class FleetbaseDeliveryProvider : IDeliveryDispatch
{
    internal const string HttpClientName = "MetroHub.Providers.Delivery.Fleetbase";
    private const int MaxErrorBodyLength = 300;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private readonly IHttpClientFactory _httpFactory;
    private readonly FleetbaseConfig _config;
    private readonly ILogger<FleetbaseDeliveryProvider> _logger;

    private string BaseUrl => _config.BaseUrl.TrimEnd('/');

    public FleetbaseDeliveryProvider(
        IHttpClientFactory httpFactory,
        FleetbaseConfig config,
        ILogger<FleetbaseDeliveryProvider> logger)
    {
        _httpFactory = httpFactory;
        _config = config;
        _logger = logger;
    }

    public async Task<string?> FindOrderAsync(string orderNumber, CancellationToken cancellationToken = default)
    {
        using var response = await SendAsync(
            HttpMethod.Get,
            $"/orders?internal_id={Uri.EscapeDataString(orderNumber)}",
            body: null,
            cancellationToken);

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
        await EnsureSuccessAsync(response, "look up order", cancellationToken);

        var raw = await response.Content.ReadAsStringAsync(cancellationToken);
        try
        {
            using var doc = JsonDocument.Parse(raw);
            // Fleetbase returns { "data": [ { "public_id": "ord_xxx", ... }, ... ] }
            if (doc.RootElement.TryGetProperty("data", out var data)
                && data.ValueKind == JsonValueKind.Array)
            {
                foreach (var order in data.EnumerateArray())
                {
                    if (order.TryGetProperty("public_id", out var pid))
                        return pid.GetString();
                }
            }
            return null;
        }
        catch (JsonException ex)
        {
            throw new DeliveryApiException("Fleetbase returned an unreadable order-lookup response.", ex);
        }
    }

    public async Task UpsertOrderAsync(DeliveryOrder order, CancellationToken cancellationToken = default)
    {
        var existingId = await FindOrderAsync(order.OrderNumber, cancellationToken);
        var payload = ToPayload(order);

        if (existingId is not null)
        {
            using var updateResp = await SendAsync(
                HttpMethod.Put, $"/orders/{existingId}", payload, cancellationToken);
            await EnsureSuccessAsync(updateResp, "update order", cancellationToken);
            return;
        }

        using var insertResp = await SendAsync(HttpMethod.Post, "/orders", payload, cancellationToken);
        await EnsureSuccessAsync(insertResp, "create order", cancellationToken);
    }

    private static FleetbasePayload ToPayload(DeliveryOrder order)
    {
        FleetbaseWaypoint? pickup = order.PickupName is not null || order.PickupAddress is not null
            ? new() { Name = order.PickupName, Street1 = order.PickupAddress, Phone = order.PickupPhone }
            : null;

        FleetbaseWaypoint? dropoff = order.CustomerAddress is not null
            ? new() { Street1 = order.CustomerAddress }
            : null;

        FleetbaseCustomer? customer = order.CustomerName is not null
            || order.CustomerEmail is not null
            || order.CustomerPhone is not null
            ? new() { Name = order.CustomerName, Email = order.CustomerEmail, Phone = order.CustomerPhone }
            : null;

        FleetbasePayloadContents? contents = order.LineItems is { Count: > 0 }
            ? new()
            {
                Entities = order.LineItems.Select(i => new FleetbaseEntity
                {
                    Name = i.Name,
                    Quantity = i.Quantity,
                    Price = i.UnitPrice,
                }).ToList(),
            }
            : null;

        return new()
        {
            InternalId = order.OrderNumber,
            Customer = customer,
            Pickup = pickup,
            Dropoff = dropoff,
            Notes = order.DriverInstruction,
            PurchaseRate = order.TotalCost,
            Payload = contents,
        };
    }

    private async Task<HttpResponseMessage> SendAsync(
        HttpMethod method, string path, object? body, CancellationToken ct)
    {
        var client = _httpFactory.CreateClient(HttpClientName);
        using var request = new HttpRequestMessage(method, BaseUrl + path);
        request.Headers.TryAddWithoutValidation("Authorization", $"Bearer {_config.ApiKey.Trim()}");
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
            throw new DeliveryApiException("Fleetbase did not respond in time (request timed out).");
        }
        catch (HttpRequestException ex)
        {
            throw new DeliveryApiException($"Fleetbase request failed: {ex.Message}", ex);
        }
    }

    private async Task EnsureSuccessAsync(HttpResponseMessage response, string action, CancellationToken ct)
    {
        if (response.IsSuccessStatusCode) return;
        var body = await response.Content.ReadAsStringAsync(ct);
        _logger.LogWarning("Fleetbase {Action} failed with HTTP {Status}.", action, (int)response.StatusCode);
        throw new DeliveryApiException(
            $"Fleetbase could not {action} (HTTP {(int)response.StatusCode}): {Truncate(body)}");
    }

    private static string Truncate(string value)
    {
        var t = value.Trim();
        return t.Length <= MaxErrorBodyLength ? t : t[..MaxErrorBodyLength] + "…";
    }
}

// Internal payload records — serialized via snake_case JsonOptions above.
internal sealed record FleetbasePayload
{
    public required string InternalId { get; init; }
    public FleetbaseCustomer? Customer { get; init; }
    public FleetbaseWaypoint? Pickup { get; init; }
    public FleetbaseWaypoint? Dropoff { get; init; }
    public string? Notes { get; init; }
    public decimal? PurchaseRate { get; init; }
    public FleetbasePayloadContents? Payload { get; init; }
}

internal sealed record FleetbaseCustomer
{
    public string? Name { get; init; }
    public string? Email { get; init; }
    public string? Phone { get; init; }
}

internal sealed record FleetbaseWaypoint
{
    public string? Name { get; init; }
    public string? Street1 { get; init; }
    public string? Phone { get; init; }
}

internal sealed record FleetbasePayloadContents
{
    public IReadOnlyList<FleetbaseEntity>? Entities { get; init; }
}

internal sealed record FleetbaseEntity
{
    public required string Name { get; init; }
    public required int Quantity { get; init; }
    public decimal? Price { get; init; }
}
