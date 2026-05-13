using InventoryService.Services;
using Microsoft.AspNetCore.Mvc;

namespace InventoryService.Controllers;

[ApiController]
[Route("inventory")]
public sealed class InventoryController : ControllerBase
{
    private readonly InventoryReservationService _service;

    public InventoryController(InventoryReservationService service)
    {
        _service = service;
    }

    [HttpGet("{sku}")]
    public async Task<IActionResult> GetInventoryAsync(
        string sku,
        CancellationToken cancellationToken)
    {
        var item = await _service.GetItemAsync(sku, cancellationToken);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost("seed")]
    public async Task<IActionResult> SeedAsync(
        [FromBody] SeedRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Sku) || request.Quantity < 0)
        {
            return BadRequest(new { message = "Invalid SKU or quantity." });
        }

        await _service.SeedAsync(request.Sku, request.Quantity, cancellationToken);
        return Ok(new { message = "Seeded inventory." });
    }

    [HttpPost("{sku}/reserve")]
    public async Task<IActionResult> ReserveAsync(
        string sku,
        [FromBody] ReserveRequest request,
        CancellationToken cancellationToken)
    {
        if (request.Quantity <= 0)
        {
            return BadRequest(new { message = "Quantity must be positive." });
        }

        var mode = ReservationMode.Naive;
        if (ReservationModeParser.TryParse(request.Mode, out var parsedMode))
        {
            mode = parsedMode;
        }

        var result = await _service.ReserveAsync(sku, request.Quantity, mode, cancellationToken);
        return result.Success ? Ok(result) : Conflict(result);
    }

    [HttpPost("{sku}/release")]
    public async Task<IActionResult> ReleaseAsync(
        string sku,
        [FromBody] ReleaseRequest request,
        CancellationToken cancellationToken)
    {
        if (request.Quantity <= 0)
        {
            return BadRequest(new { message = "Quantity must be positive." });
        }

        var result = await _service.ReleaseAsync(sku, request.Quantity, cancellationToken);
        return result.Success ? Ok(result) : Conflict(result);
    }
}

public sealed record SeedRequest(string Sku, int Quantity);

public sealed record ReserveRequest(int Quantity, string? Mode);

public sealed record ReleaseRequest(int Quantity);
