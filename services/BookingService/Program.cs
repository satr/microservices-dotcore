using BookingService.Consumers;
using BookingService.Data;
using BookingService.Messaging;
using BookingService.Repositories;
using BookingService.Services;
using Library.Contracts.Messages;
using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using OpenTelemetry.Exporter;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services
    .AddApiVersioning(options =>
    {
        options.DefaultApiVersion = new Asp.Versioning.ApiVersion(1, 0);
        options.AssumeDefaultVersionWhenUnspecified = true;
        options.ReportApiVersions = true;
    })
    .AddMvc()
    .AddApiExplorer(options =>
    {
        options.GroupNameFormat = "'v'VVV";
        options.SubstituteApiVersionInUrl = true;
    });

builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService("booking-service"))
    .WithTracing(tracing =>
    {
        tracing
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddSource("MassTransit")
            .AddOtlpExporter(options =>
            {
                options.Endpoint = new Uri(builder.Configuration["OpenTelemetry:OtlpEndpoint"] ?? "http://jaeger:4317");
                options.Protocol = OtlpExportProtocol.Grpc;
            });
    });

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

builder.Services.AddSingleton<IBookInventoryService, BookInventoryService>();

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = builder.Configuration["Keycloak:Authority"]
                            ?? "http://localhost:8888/realms/library";
        options.MetadataAddress = builder.Configuration["Keycloak:MetadataAddress"]
                                  ?? "http://localhost:8888/realms/library/.well-known/openid-configuration";
        options.RequireHttpsMetadata = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidIssuer   = builder.Configuration["Keycloak:Authority"] ?? "http://localhost:8888/realms/library",
            RoleClaimType = "roles",
            NameClaimType = "preferred_username"
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("MemberOrLibrarian", policy => policy.RequireRole("member", "librarian"));
    options.AddPolicy("LibrarianOnly",     policy => policy.RequireRole("librarian"));
});

var messagingProvider = builder.Configuration["Messaging:Provider"] ?? "RabbitMq";

builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<CartItemAddedConsumer>();
    x.AddConsumer<CartItemRemovedConsumer>();
    x.AddConsumer<BorrowingCompletedConsumer>();
    x.AddConsumer<AddToCartFailedConsumer>();

    if (messagingProvider == "Kafka")
    {
        var kafkaBootstrap = builder.Configuration["Kafka:BootstrapServers"] ?? "kafka:9092";
        var topicPrefix   = builder.Configuration["Kafka:TopicPrefix"] ?? "library";

        // InMemory bus keeps MassTransit pipeline intact; Kafka Rider handles real I/O
        x.UsingInMemory((context, cfg) => cfg.ConfigureEndpoints(context));

        x.AddRider(rider =>
        {
            // Register Kafka producers (used by KafkaCartCommandPublisher)
            rider.AddProducer<AddToCartRequested>($"{topicPrefix}.add-to-cart-requested");
            rider.AddProducer<RemoveFromCartRequested>($"{topicPrefix}.remove-from-cart-requested");
            rider.AddProducer<CompleteBorrowingRequested>($"{topicPrefix}.complete-borrowing-requested");

            // Register consumers for outcome events published by workflow-saga
            rider.AddConsumer<CartItemAddedConsumer>();
            rider.AddConsumer<CartItemRemovedConsumer>();
            rider.AddConsumer<BorrowingCompletedConsumer>();
            rider.AddConsumer<AddToCartFailedConsumer>();

            rider.UsingKafka((context, k) =>
            {
                k.Host(kafkaBootstrap);

                k.TopicEndpoint<CartItemAdded>($"{topicPrefix}.cart-item-added", "booking-service", e =>
                {
                    e.AutoOffsetReset = Confluent.Kafka.AutoOffsetReset.Earliest;
                    e.ConfigureConsumer<CartItemAddedConsumer>(context);
                });
                k.TopicEndpoint<CartItemRemoved>($"{topicPrefix}.cart-item-removed", "booking-service", e =>
                {
                    e.AutoOffsetReset = Confluent.Kafka.AutoOffsetReset.Earliest;
                    e.ConfigureConsumer<CartItemRemovedConsumer>(context);
                });
                k.TopicEndpoint<BorrowingCompleted>($"{topicPrefix}.borrowing-completed", "booking-service", e =>
                {
                    e.AutoOffsetReset = Confluent.Kafka.AutoOffsetReset.Earliest;
                    e.ConfigureConsumer<BorrowingCompletedConsumer>(context);
                });
                k.TopicEndpoint<AddToCartFailed>($"{topicPrefix}.add-to-cart-failed", "booking-service", e =>
                {
                    e.AutoOffsetReset = Confluent.Kafka.AutoOffsetReset.Earliest;
                    e.ConfigureConsumer<AddToCartFailedConsumer>(context);
                });
            });
        });

        builder.Services.AddScoped<ICartCommandPublisher, KafkaCartCommandPublisher>();
    }
    else
    {
        x.UsingRabbitMq((context, cfg) =>
        {
            cfg.Host(builder.Configuration["RabbitMq:Host"] ?? "rabbitmq", "/", host =>
            {
                host.Username(builder.Configuration["RabbitMq:Username"] ?? "guest");
                host.Password(builder.Configuration["RabbitMq:Password"] ?? "guest");
            });

            cfg.ConfigureEndpoints(context);
        });

        builder.Services.AddScoped<ICartCommandPublisher, RabbitMqCartCommandPublisher>();
    }
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

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
