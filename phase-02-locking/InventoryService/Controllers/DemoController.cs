using InventoryService.Demos;
using InventoryService.Services;
using Microsoft.AspNetCore.Mvc;

namespace InventoryService.Controllers;

[ApiController]
[Route("demo")]
public sealed class DemoController : ControllerBase
{
    private readonly ReservationLoadTest _loadTest;
    private readonly InMemoryLockingDemo _inMemoryDemo;

    public DemoController(
        ReservationLoadTest loadTest,
        InMemoryLockingDemo inMemoryDemo)
    {
        _loadTest = loadTest;
        _inMemoryDemo = inMemoryDemo;
    }

    [HttpPost("loadtest")]
    public async Task<IActionResult> RunLoadTestAsync(
        [FromBody] LoadTestRequest request,
        CancellationToken cancellationToken)
    {
        var mode = ReservationMode.Naive;
        if (ReservationModeParser.TryParse(request.Mode, out var parsedMode))
        {
            mode = parsedMode;
        }

        var sku = string.IsNullOrWhiteSpace(request.Sku) ? "SKU-1" : request.Sku;
        var initialQuantity = request.InitialQuantity <= 0 ? 1 : request.InitialQuantity;
        var reservationQuantity = request.ReservationQuantity <= 0 ? 1 : request.ReservationQuantity;
        var requestCount = request.RequestCount <= 0 ? 50 : request.RequestCount;

        var result = await _loadTest.RunAsync(
            sku,
            initialQuantity,
            reservationQuantity,
            requestCount,
            mode,
            cancellationToken);

        return Ok(result);
    }

    [HttpPost("inmemory-optimistic")]
    public async Task<IActionResult> RunInMemoryOptimisticAsync(
        [FromBody] InMemoryDemoRequest request,
        CancellationToken cancellationToken)
    {
        var initialQuantity = request.InitialQuantity <= 0 ? 1 : request.InitialQuantity;
        var requestCount = request.RequestCount <= 0 ? 50 : request.RequestCount;

        var result = await _inMemoryDemo.RunOptimisticAsync(
            initialQuantity,
            requestCount,
            cancellationToken);

        return Ok(result);
    }

    [HttpPost("inmemory-pessimistic")]
    public async Task<IActionResult> RunInMemoryPessimisticAsync(
        [FromBody] InMemoryDemoRequest request,
        CancellationToken cancellationToken)
    {
        var initialQuantity = request.InitialQuantity <= 0 ? 1 : request.InitialQuantity;
        var requestCount = request.RequestCount <= 0 ? 50 : request.RequestCount;

        var result = await _inMemoryDemo.RunPessimisticAsync(
            initialQuantity,
            requestCount,
            cancellationToken);

        return Ok(result);
    }
}

public sealed record LoadTestRequest(
    string? Sku,
    int InitialQuantity,
    int ReservationQuantity,
    int RequestCount,
    string? Mode);

public sealed record InMemoryDemoRequest(int InitialQuantity, int RequestCount);
