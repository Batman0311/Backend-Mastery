// Service layer types.
using InventoryService.Services;
// MVC attributes and base types.
using Microsoft.AspNetCore.Mvc;

// Namespace for API controllers.
namespace InventoryService.Controllers;

// Marks this class as an API controller.
[ApiController]
// Base route for inventory endpoints.
[Route("inventory")]
// Controller for inventory CRUD-style operations.
public sealed class InventoryController : ControllerBase
{
    // Reservation service used by the controller.
    private readonly InventoryReservationService _service;

    // Inject the reservation service.
    public InventoryController(InventoryReservationService service)
    {
        _service = service;
    }

    // Gets the current inventory state for a SKU.
    [HttpGet("{sku}")]
    public async Task<IActionResult> GetInventoryAsync(
        // SKU from the route.
        string sku,
        // Cancellation token from the request.
        CancellationToken cancellationToken)
    {
        // Fetch the item.
        var item = await _service.GetItemAsync(sku, cancellationToken);
        // Return 404 if missing, otherwise 200.
        return item is null ? NotFound() : Ok(item);
    }

    // Seeds inventory for a SKU.
    [HttpPost("seed")]
    public async Task<IActionResult> SeedAsync(
        // Request payload.
        [FromBody] SeedRequest request,
        // Cancellation token from the request.
        CancellationToken cancellationToken)
    {
        // Validate request.
        if (string.IsNullOrWhiteSpace(request.Sku) || request.Quantity < 0)
        {
            return BadRequest(new { message = "Invalid SKU or quantity." });
        }

        // Perform the seed.
        await _service.SeedAsync(request.Sku, request.Quantity, cancellationToken);
        // Respond with confirmation.
        return Ok(new { message = "Seeded inventory." });
    }

    // Reserves stock for a SKU.
    [HttpPost("{sku}/reserve")]
    public async Task<IActionResult> ReserveAsync(
        // SKU from the route.
        string sku,
        // Request payload.
        [FromBody] ReserveRequest request,
        // Cancellation token from the request.
        CancellationToken cancellationToken)
    {
        // Validate request.
        if (request.Quantity <= 0)
        {
            return BadRequest(new { message = "Quantity must be positive." });
        }

        // Default to naive mode if parsing fails.
        var mode = ReservationMode.Naive;
        // Try to parse the requested mode.
        if (ReservationModeParser.TryParse(request.Mode, out var parsedMode))
        {
            mode = parsedMode;
        }

        // Build request context for logs/metrics.
        var context = new RequestContext(
            GetCorrelationId(Request),
            HttpContext.TraceIdentifier,
            mode.ToString());

        // Execute the reservation.
        var result = await _service.ReserveAsync(sku, request.Quantity, mode, context, cancellationToken);
        // Return 200 on success or 409 on conflict.
        return result.Success ? Ok(result) : Conflict(result);
    }

    // Releases previously reserved stock for a SKU.
    [HttpPost("{sku}/release")]
    public async Task<IActionResult> ReleaseAsync(
        // SKU from the route.
        string sku,
        // Request payload.
        [FromBody] ReleaseRequest request,
        // Cancellation token from the request.
        CancellationToken cancellationToken)
    {
        // Validate request.
        if (request.Quantity <= 0)
        {
            return BadRequest(new { message = "Quantity must be positive." });
        }

        // Build request context for logs/metrics.
        var context = new RequestContext(
            GetCorrelationId(Request),
            HttpContext.TraceIdentifier,
            "release");

        // Execute the release.
        var result = await _service.ReleaseAsync(sku, request.Quantity, context, cancellationToken);
        // Return 200 on success or 409 on conflict.
        return result.Success ? Ok(result) : Conflict(result);
    }

    // Resolve correlation id from header or create a new one.
    private static string GetCorrelationId(HttpRequest request)
    {
        // Use header value when present.
        if (request.Headers.TryGetValue("X-Correlation-Id", out var values)
            && !string.IsNullOrWhiteSpace(values.FirstOrDefault()))
        {
            return values.First();
        }

        // Fall back to a generated correlation id.
        return Guid.NewGuid().ToString("N");
    }
}

// Request body for seeding inventory.
public sealed record SeedRequest(
    // SKU to seed.
    string Sku,
    // Quantity to set.
    int Quantity);

// Request body for reservations.
public sealed record ReserveRequest(
    // Quantity to reserve.
    int Quantity,
    // Demo mode name.
    string? Mode);

// Request body for releases.
public sealed record ReleaseRequest(
    // Quantity to release.
    int Quantity);
