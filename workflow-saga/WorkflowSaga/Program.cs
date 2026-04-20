using Library.Contracts.Messages;
using MassTransit;
using OpenTelemetry.Exporter;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using WorkflowSaga.Saga;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService("workflow-saga"))
    .WithTracing(tracing =>
    {
        tracing
            .AddSource("MassTransit")
            .AddOtlpExporter(options =>
            {
                options.Endpoint = new Uri(builder.Configuration["OpenTelemetry:OtlpEndpoint"] ?? "http://jaeger:4317");
                options.Protocol = OtlpExportProtocol.Grpc;
            });
    });

var messagingProvider = builder.Configuration["Messaging:Provider"] ?? "RabbitMq";

builder.Services.AddMassTransit(x =>
{
    x.AddSagaStateMachine<BorrowingStateMachine, BorrowingState>()
        .InMemoryRepository();

    if (messagingProvider == "Kafka")
    {
        var kafkaBootstrap = builder.Configuration["Kafka:BootstrapServers"] ?? "kafka:9092";
        var topicPrefix   = builder.Configuration["Kafka:TopicPrefix"] ?? "library";

        // InMemory bus keeps MassTransit pipeline intact; Kafka Rider handles real I/O
        x.UsingInMemory((context, cfg) => cfg.ConfigureEndpoints(context));

        x.AddRider(rider =>
        {
            // Producers for outcome events (consumed by booking-service)
            rider.AddProducer<CartItemAdded>($"{topicPrefix}.cart-item-added");
            rider.AddProducer<CartItemRemoved>($"{topicPrefix}.cart-item-removed");
            rider.AddProducer<BorrowingCompleted>($"{topicPrefix}.borrowing-completed");
            rider.AddProducer<AddToCartFailed>($"{topicPrefix}.add-to-cart-failed");

            // Saga receives command topics published by booking-service
            rider.AddSagaStateMachine<BorrowingStateMachine, BorrowingState>()
                .InMemoryRepository();

            rider.UsingKafka((context, k) =>
            {
                k.Host(kafkaBootstrap);

                k.TopicEndpoint<AddToCartRequested>($"{topicPrefix}.add-to-cart-requested", "workflow-saga", e =>
                {
                    e.AutoOffsetReset = Confluent.Kafka.AutoOffsetReset.Earliest;
                    e.ConfigureSaga<BorrowingState>(context);
                });
                k.TopicEndpoint<RemoveFromCartRequested>($"{topicPrefix}.remove-from-cart-requested", "workflow-saga", e =>
                {
                    e.AutoOffsetReset = Confluent.Kafka.AutoOffsetReset.Earliest;
                    e.ConfigureSaga<BorrowingState>(context);
                });
                k.TopicEndpoint<CompleteBorrowingRequested>($"{topicPrefix}.complete-borrowing-requested", "workflow-saga", e =>
                {
                    e.AutoOffsetReset = Confluent.Kafka.AutoOffsetReset.Earliest;
                    e.ConfigureSaga<BorrowingState>(context);
                });
            });
        });
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
    }
});

if (messagingProvider == "Kafka")
    builder.Services.AddSingleton<IBorrowingEventPublisher, KafkaBorrowingEventPublisher>();
else
    builder.Services.AddSingleton<IBorrowingEventPublisher, RabbitMqBorrowingEventPublisher>();

var host = builder.Build();
host.Run();
