using BookingService.Consumers;
using BookingService.Data;
using BookingService.Repositories;
using BookingService.Services;
using MassTransit;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (!string.IsNullOrWhiteSpace(connectionString))
{
    builder.Services.AddDbContext<BookingDbContext>(opt =>
        opt.UseNpgsql(connectionString));
    builder.Services.AddDbContext<BookingInventoryDbContext>(opt =>
        opt.UseNpgsql(connectionString));
    builder.Services.AddSingleton<ICartRepository, PostgresCartRepository>();
}
else
{
    builder.Services.AddSingleton<ICartRepository, InMemoryCartRepository>();
}

builder.Services.AddSingleton<IBookInventoryService, BookInventoryService>();

builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<CartItemAddedConsumer>();
    x.AddConsumer<CartItemRemovedConsumer>();
    x.AddConsumer<BorrowingCompletedConsumer>();
    x.AddConsumer<AddToCartFailedConsumer>();
    x.AddConsumer<CartItemRemovalConfirmedConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(builder.Configuration["RabbitMq:Host"] ?? "rabbitmq", "/", host =>
        {
            host.Username(builder.Configuration["RabbitMq:Username"] ?? "guest");
            host.Password(builder.Configuration["RabbitMq:Password"] ?? "guest");
        });

        cfg.ConfigureEndpoints(context);
    });
});

var app = builder.Build();

// Apply EF migrations on startup when using PostgreSQL.
if (!string.IsNullOrWhiteSpace(connectionString))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<BookingDbContext>();
    var inventoryDb = scope.ServiceProvider.GetRequiredService<BookingInventoryDbContext>();
    db.Database.Migrate();
    inventoryDb.Database.Migrate();
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseAuthorization();
app.MapControllers();

app.Run();






