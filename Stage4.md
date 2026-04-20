# Stage 4: API and Schema Versioning

## Scope Covered

1. **4.1 API Versioning** — URL-segment versioning on all three services, version metadata in responses, `MessageEnvelope<T>` for Kafka message schema versioning
2. **4.2 Protobuf / Schema Registry** — `.proto` definitions for all message types, Confluent Schema Registry container, `make schema-check` / `make schema-set-full-transitive` targets

---

## What Was Implemented

### 4.1 API Versioning

#### Package changes

`Asp.Versioning.Mvc` and `Asp.Versioning.Mvc.ApiExplorer` (`8.1.0`) added to:
- `services/BooksService/BooksService.csproj`
- `services/UsersService/UsersService.csproj`
- `services/BookingService/BookingService.csproj`

#### Program.cs — all three services

```csharp
builder.Services
    .AddApiVersioning(options =>
    {
        options.DefaultApiVersion = new ApiVersion(1, 0);
        options.AssumeDefaultVersionWhenUnspecified = true;
        options.ReportApiVersions = true; // adds api-supported-versions response header
    })
    .AddMvc()
    .AddApiExplorer(options =>
    {
        options.GroupNameFormat           = "'v'VVV";
        options.SubstituteApiVersionInUrl = true;
    });
```

#### Controller routes

| Service | Old route | New route |
|---|---|---|
| books-service | `GET /api/books/search` | `GET /api/v1/books/search` |
| books-service | `GET /api/books/{id}` | `GET /api/v1/books/{id}` |
| users-service | `GET /api/users/by-name/{name}` | `GET /api/v1/users/by-name/{name}` |
| booking-service | `*  /api/cart/*` | `*  /api/v1/cart/*` |

All controllers annotated with `[ApiVersion("1.0")]` and route template `api/v{version:apiVersion}/...`.

#### Response headers (automatic via `ReportApiVersions = true`)

```
api-supported-versions: 1.0
```

When a version is deprecated, add `options.ApiVersioningErrorResponses.IsEnabled = true` and annotate old controllers with `[ApiVersion("1.0", Deprecated = true)]` — the framework will automatically emit:

```
api-deprecated-versions: 1.0
Sunset: <RFC7231 date>
```

#### Frontend updated

All `HttpClient` calls in `frontend/LibraryWeb/Pages/Index.cshtml.cs` and `Cart.cshtml.cs` updated from `/api/<resource>/` to `/api/v1/<resource>/`.

#### MessageEnvelope<T> — Kafka contract versioning

Added to `shared/Library.Contracts/Class1.cs`:

```csharp
public sealed record MessageEnvelope<T>(
    int SchemaVersion,
    string MessageType,
    T Payload)
{
    public static MessageEnvelope<T> V1(T payload) =>
        new(1, typeof(T).Name, payload);
}
```

Usage when publishing to Kafka:
```csharp
var envelope = MessageEnvelope<AddToCartRequested>.V1(message);
// publish envelope instead of raw message
```

Consumers inspect `SchemaVersion` before deserialising `Payload` and can reject or upgrade unknown versions without crashing.

---

### 4.2 Protobuf / Schema Registry

#### Proto definitions

All message types defined in `shared/Library.Contracts/Proto/library_messages.proto` with `csharp_namespace = "Library.Contracts.Proto"`:

| Message | Direction |
|---|---|
| `AddToCartRequested` | booking-service → workflow-saga |
| `RemoveFromCartRequested` | booking-service → workflow-saga |
| `CompleteBorrowingRequested` | booking-service → workflow-saga |
| `CartItemAdded` | workflow-saga → booking-service |
| `CartItemRemoved` | workflow-saga → booking-service |
| `BorrowingCompleted` | workflow-saga → booking-service |
| `AddToCartFailed` | workflow-saga → booking-service |
| `BookStockRestored` | internal |

`Library.Contracts.csproj` updated with `Google.Protobuf` (`3.29.3`) and `Grpc.Tools` (`2.70.0`) — C# classes are generated at build time from `Proto/*.proto`.

#### Schema Registry container

Added to `docker-compose.kafka.yml`:

```yaml
schema-registry:
  image: confluentinc/cp-schema-registry:7.7.1
  ports:
    - "8082:8081"           # host:container — host 8082 avoids clash with kafka-ui
  environment:
    SCHEMA_REGISTRY_KAFKASTORE_BOOTSTRAP_SERVERS: kafka:9092
    SCHEMA_REGISTRY_HOST_NAME: schema-registry
    SCHEMA_REGISTRY_LISTENERS: http://0.0.0.0:8081
```

Kafka UI configured to show Schema Registry at `http://schema-registry:8081`.

#### Makefile targets

```bash
# Start Kafka + Schema Registry + Kafka UI only
make kafka-up

# List all registered subjects + global compatibility mode
make schema-check

# Set FULL_TRANSITIVE compatibility (breaking schema changes blocked)
make schema-set-full-transitive

# Start full stack in Kafka mode (includes Schema Registry)
make kafka-stack-up
```

---

## How to Inspect and Use Schema Registry

### List registered schemas

```bash
make schema-check
# or directly:
curl http://localhost:8082/subjects
```

### Register a schema manually (example — AddToCartRequested)

```bash
curl -X POST http://localhost:8082/subjects/library.add-to-cart-requested-value/versions \
  -H "Content-Type: application/vnd.schemaregistry.v1+json" \
  -d '{
    "schemaType": "PROTOBUF",
    "schema": "syntax = \"proto3\"; message AddToCartRequested { string correlation_id = 1; string user_id = 2; string book_id = 3; string title = 4; string author = 5; string requested_at_utc = 6; }"
  }'
```

### Check compatibility before evolving a schema

```bash
# Test whether a new schema version is compatible with what is registered
curl -X POST http://localhost:8082/compatibility/subjects/library.add-to-cart-requested-value/versions/latest \
  -H "Content-Type: application/vnd.schemaregistry.v1+json" \
  -d '{ "schemaType": "PROTOBUF", "schema": "..." }'
```

### Enforce FULL_TRANSITIVE compatibility

```bash
make schema-set-full-transitive
# Equivalent to:
curl -X PUT http://localhost:8082/config \
  -H "Content-Type: application/vnd.schemaregistry.v1+json" \
  -d '{"compatibility":"FULL_TRANSITIVE"}'
```

`FULL_TRANSITIVE` means a new schema must be both forward and backward compatible with **all** previous versions — the safest mode for event-driven systems where consumers may lag producers.

---

## How to Add a v2 API Version (learning exercise)

1. Create `Controllers/V2/BooksController.cs` annotated with `[ApiVersion("2.0")]` and `[Route("api/v{version:apiVersion}/books")]`.
2. Register v2 in `Program.cs` (the `AddApiVersioning` call already supports multiple versions).
3. Mark the v1 controller deprecated:
   ```csharp
   [ApiVersion("1.0", Deprecated = true)]
   ```
4. The framework will start returning `api-deprecated-versions: 1.0` on every v1 response.
5. Add a `Sunset` header via middleware to give clients a removal date:
   ```csharp
   app.Use(async (ctx, next) =>
   {
       await next();
       if (ctx.Request.Path.StartsWithSegments("/api/v1"))
           ctx.Response.Headers["Sunset"] = "Sat, 01 Jan 2027 00:00:00 GMT";
   });
   ```

---

## Notes

- Proto-generated C# classes (`Library.Contracts.Proto.*`) are compiled at build time — no manual code generation step needed.
- Full Kafka protobuf serialization via `Confluent.SchemaRegistry.Serdes.Protobuf` + MassTransit custom serializer is defined in Stage 4 Hardening scope.
- The `MessageEnvelope<T>` wrapper is available now for any producer that wants to add schema version metadata without waiting for full protobuf adoption.

