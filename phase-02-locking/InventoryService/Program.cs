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
builder.Services.AddControllers();
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

app.MapControllers();

app.Run();
