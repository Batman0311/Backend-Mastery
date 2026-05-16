// Data layer types.
using InventoryService.Data;
// Demo runners.
using InventoryService.Demos;
// Lock providers.
using InventoryService.Locking;
// Service layer.
using InventoryService.Services;
// EF Core provider for SQLite.
using Microsoft.EntityFrameworkCore;
// Redis connection.
using StackExchange.Redis;

// Create the web application builder.
var builder = WebApplication.CreateBuilder(args);

// Register EF Core DbContext for scoped usage.
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("InventoryDb")));
// Register a DbContextFactory for parallel/demo usage.
builder.Services.AddDbContextFactory<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("InventoryDb")));

// Register a local lock provider (used to simulate per-instance locks).
builder.Services.AddSingleton<IDistributedLockProvider, InProcessDistributedLockProvider>();
// Register a shared Redis connection.
builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
    ConnectionMultiplexer.Connect(builder.Configuration.GetConnectionString("Redis")
        ?? "localhost:6379"));
// Register the Redis lock provider for distributed locking.
builder.Services.AddSingleton<RedisDistributedLockProvider>();

// Register core reservation service.
builder.Services.AddScoped<InventoryReservationService>();
// Register demo runner for single-instance load tests.
builder.Services.AddScoped<ReservationLoadTest>();
// Register demo runner for cross-instance simulations.
builder.Services.AddScoped<CrossInstanceLoadTest>();

// Add MVC controllers.
builder.Services.AddControllers();
// Add endpoint metadata for Swagger.
builder.Services.AddEndpointsApiExplorer();
// Add Swagger generation.
builder.Services.AddSwaggerGen();

// Build the app.
var app = builder.Build();

// Ensure the SQLite database exists on startup.
using (var scope = app.Services.CreateScope())
{
    // Resolve the DbContext.
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    // Create the database file if missing.
    db.Database.EnsureCreated();
}

// Enable Swagger UI in development only.
if (app.Environment.IsDevelopment())
{
    // Serve OpenAPI JSON.
    app.UseSwagger();
    // Serve Swagger UI.
    app.UseSwaggerUI();
}

// Redirect HTTP to HTTPS.
app.UseHttpsRedirection();

// Map controller routes.
app.MapControllers();

// Start the web app.
app.Run();
