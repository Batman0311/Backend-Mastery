// Demo runner types.
using InventoryService.Demos;
// Service-layer enums.
using InventoryService.Services;
// MVC attributes and base types.
using Microsoft.AspNetCore.Mvc;

// Namespace for API controllers.
namespace InventoryService.Controllers;

// Marks this class as an API controller.
[ApiController]
// Base route for demo endpoints.
[Route("demo")]
// Controller hosting demo endpoints.
public sealed class DemoController : ControllerBase
{
    // Single-instance load test runner.
    private readonly ReservationLoadTest _loadTest;
    // Cross-instance load test runner.
    private readonly CrossInstanceLoadTest _crossInstanceLoadTest;

    // Inject demo runners.
    public DemoController(
        ReservationLoadTest loadTest,
        CrossInstanceLoadTest crossInstanceLoadTest)
    {
        _loadTest = loadTest;
        _crossInstanceLoadTest = crossInstanceLoadTest;
    }

    // Runs a single-instance load test.
    [HttpPost("loadtest")]
    public async Task<IActionResult> RunLoadTestAsync(
        // Request payload with test configuration.
        [FromBody] LoadTestRequest request,
        // Cancellation token from the request.
        CancellationToken cancellationToken)
    {
        // Default to naive mode when parsing fails.
        var mode = ReservationMode.Naive;
        // Try to parse the requested mode.
        if (ReservationModeParser.TryParse(request.Mode, out var parsedMode))
        {
            mode = parsedMode;
        }

        // Provide defaults for missing or invalid values.
        var sku = string.IsNullOrWhiteSpace(request.Sku) ? "SKU-1" : request.Sku;
        // Default initial quantity to 1.
        var initialQuantity = request.InitialQuantity <= 0 ? 1 : request.InitialQuantity;
        // Default reservation quantity to 1.
        var reservationQuantity = request.ReservationQuantity <= 0 ? 1 : request.ReservationQuantity;
        // Default request count to 50.
        var requestCount = request.RequestCount <= 0 ? 50 : request.RequestCount;

        // Run the demo workload.
        var result = await _loadTest.RunAsync(
            sku,
            initialQuantity,
            reservationQuantity,
            requestCount,
            mode,
            cancellationToken);

        // Return the summary result.
        return Ok(result);
    }

    // Runs a cross-instance load test.
    [HttpPost("cross-instance")]
    public async Task<IActionResult> RunCrossInstanceAsync(
        // Request payload with test configuration.
        [FromBody] LoadTestRequest request,
        // Cancellation token from the request.
        CancellationToken cancellationToken)
    {
        // Default to cross-instance local mode.
        var mode = ReservationMode.CrossInstanceLocal;
        // Try to parse the requested mode.
        if (ReservationModeParser.TryParse(request.Mode, out var parsedMode))
        {
            mode = parsedMode;
        }

        // Provide defaults for missing or invalid values.
        var sku = string.IsNullOrWhiteSpace(request.Sku) ? "SKU-1" : request.Sku;
        // Default initial quantity to 1.
        var initialQuantity = request.InitialQuantity <= 0 ? 1 : request.InitialQuantity;
        // Default reservation quantity to 1.
        var reservationQuantity = request.ReservationQuantity <= 0 ? 1 : request.ReservationQuantity;
        // Default request count to 50.
        var requestCount = request.RequestCount <= 0 ? 50 : request.RequestCount;

        // Run the cross-instance demo workload.
        var result = await _crossInstanceLoadTest.RunAsync(
            sku,
            initialQuantity,
            reservationQuantity,
            requestCount,
            mode,
            cancellationToken);

        // Return the summary result.
        return Ok(result);
    }
}

// Request body for demo load tests.
public sealed record LoadTestRequest(
    // SKU to test.
    string? Sku,
    // Initial quantity to seed.
    int InitialQuantity,
    // Quantity reserved per request.
    int ReservationQuantity,
    // Number of concurrent requests.
    int RequestCount,
    // Requested demo mode.
    string? Mode);
