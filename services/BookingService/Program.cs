using BookingService.Consumers;
using BookingService.Data;
using BookingService.Repositories;
using MassTransit;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (!string.IsNullOrWhiteSpace(connectionString))
{
    builder.Services.AddDbContext<BookingDbContext>(opt =>
        opt.UseNpgsql(connectionString));
    builder.Services.AddSingleton<ICartRepository, PostgresCartRepository>();
}
else
{
    builder.Services.AddSingleton<ICartRepository, InMemoryCartRepository>();
}

builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<CartItemAddedConsumer>();
    x.AddConsumer<CartItemRemovedConsumer>();
    x.AddConsumer<BorrowingCompletedConsumer>();

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
    db.Database.Migrate();
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseAuthorization();
app.MapControllers();

app.Run();


