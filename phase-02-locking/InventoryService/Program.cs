using InventoryService.Data;
using InventoryService.Demos;
using InventoryService.Locking;
using InventoryService.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("InventoryDb")));
builder.Services.AddSingleton<IDistributedLockProvider, InMemoryDistributedLockProvider>();
builder.Services.AddScoped<InventoryReservationService>();
builder.Services.AddScoped<ReservationLoadTest>();
builder.Services.AddScoped<InMemoryLockingDemo>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapGet("/inventory/{sku}", async (
    string sku,
    InventoryReservationService service,
    CancellationToken cancellationToken) =>
{
    var item = await service.GetItemAsync(sku, cancellationToken);
    return item is null ? Results.NotFound() : Results.Ok(item);
});

app.MapPost("/inventory/seed", async (
    SeedRequest request,
    InventoryReservationService service,
    CancellationToken cancellationToken) =>
{
    if (string.IsNullOrWhiteSpace(request.Sku) || request.Quantity < 0)
    {
        return Results.BadRequest(new { message = "Invalid SKU or quantity." });
    }

    await service.SeedAsync(request.Sku, request.Quantity, cancellationToken);
    return Results.Ok(new { message = "Seeded inventory." });
});

app.MapPost("/inventory/{sku}/reserve", async (
    string sku,
    ReserveRequest request,
    InventoryReservationService service,
    CancellationToken cancellationToken) =>
{
    if (request.Quantity <= 0)
    {
        return Results.BadRequest(new { message = "Quantity must be positive." });
    }

    var mode = ReservationMode.Naive;
    if (ReservationModeParser.TryParse(request.Mode, out var parsedMode))
    {
        mode = parsedMode;
    }

    var result = await service.ReserveAsync(sku, request.Quantity, mode, cancellationToken);
    return result.Success ? Results.Ok(result) : Results.Conflict(result);
});

app.MapPost("/inventory/{sku}/release", async (
    string sku,
    ReleaseRequest request,
    InventoryReservationService service,
    CancellationToken cancellationToken) =>
{
    if (request.Quantity <= 0)
    {
        return Results.BadRequest(new { message = "Quantity must be positive." });
    }

    var result = await service.ReleaseAsync(sku, request.Quantity, cancellationToken);
    return result.Success ? Results.Ok(result) : Results.Conflict(result);
});

app.MapPost("/demo/loadtest", async (
    LoadTestRequest request,
    ReservationLoadTest demo,
    CancellationToken cancellationToken) =>
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

    var result = await demo.RunAsync(
        sku,
        initialQuantity,
        reservationQuantity,
        requestCount,
        mode,
        cancellationToken);

    return Results.Ok(result);
});

app.MapPost("/demo/inmemory-optimistic", async (
    InMemoryDemoRequest request,
    InMemoryLockingDemo demo,
    CancellationToken cancellationToken) =>
{
    var initialQuantity = request.InitialQuantity <= 0 ? 1 : request.InitialQuantity;
    var requestCount = request.RequestCount <= 0 ? 50 : request.RequestCount;

    var result = await demo.RunOptimisticAsync(
        initialQuantity,
        requestCount,
        cancellationToken);

    return Results.Ok(result);
});

app.MapPost("/demo/inmemory-pessimistic", async (
    InMemoryDemoRequest request,
    InMemoryLockingDemo demo,
    CancellationToken cancellationToken) =>
{
    var initialQuantity = request.InitialQuantity <= 0 ? 1 : request.InitialQuantity;
    var requestCount = request.RequestCount <= 0 ? 50 : request.RequestCount;

    var result = await demo.RunPessimisticAsync(
        initialQuantity,
        requestCount,
        cancellationToken);

    return Results.Ok(result);
});

app.Run();

public sealed record SeedRequest(string Sku, int Quantity);

public sealed record ReserveRequest(int Quantity, string? Mode);

public sealed record ReleaseRequest(int Quantity);

public sealed record LoadTestRequest(
    string? Sku,
    int InitialQuantity,
    int ReservationQuantity,
    int RequestCount,
    string? Mode);

public sealed record InMemoryDemoRequest(int InitialQuantity, int RequestCount);
