# Stage 2: Resilience, Tracing, and Stock-Aware Saga

## Scope Covered

This stage now includes the set of Stage 2 requirements:

1. Resilience patterns (retry / circuit-breaker direction)
2. Distributed tracing (Jaeger integration foundation)
3. Dedicated saga service placement (`workflow-saga`)
4. Stock-aware booking flow with failure events

---

## What Was Implemented

### 1) Resilience patterns

- `frontend` now uses Polly retry policy for calls to `booking-service`.
- Retry behavior: 3 retries with exponential backoff.
- A failure simulation path was added so transient failures can be observed during booking flow tests.

Relevant files:
- `frontend/LibraryWeb/Pages/Index.cshtml.cs`
- `frontend/LibraryWeb/Program.cs`
- `services/BookingService/Services/BookInventoryService.cs`

---

### 2) Distributed tracing (Jaeger)

- Jaeger service was added to container orchestration.
- Service environment variables for Jaeger agent host/port were added in compose configuration.
- OpenTelemetry/Jaeger package dependencies were added as a tracing foundation.

Relevant files:
- `docker-compose.yml`
- `services/BookingService/appsettings.json`
- `services/*/*.csproj` and `frontend/LibraryWeb/LibraryWeb.csproj` (package references)

---

### 3) Saga placement: dedicated workflow service

- Saga remains in the separate `.NET` service: `workflow-saga/WorkflowSaga`.
- This keeps orchestration outside feature APIs (`users`, `books`, `booking`) and follows the planned separation of concerns.

Relevant files:
- `workflow-saga/WorkflowSaga/Saga/BorrowingStateMachine.cs`
- `workflow-saga/WorkflowSaga/Program.cs`

---

### 4) Stock-aware booking behavior

- Booking inventory model was introduced with per-book stock (`default = 10`).
- Stock data is managed in booking persistence context.
- Saga flow now supports add-to-cart failure signaling when stock checks fail.
- New events and consumers were added to handle success/failure paths and cart-removal confirmation.

Relevant files:
- `services/BookingService/Data/BookingInventoryDbContext.cs`
- `services/BookingService/Services/BookInventoryService.cs`
- `shared/Library.Contracts/Class1.cs`
- `services/BookingService/Consumers/AddToCartFailedConsumer.cs`
- `workflow-saga/WorkflowSaga/Saga/BorrowingStateMachine.cs`

---

## PostgreSQL status (kept from earlier Stage 2 work)

- Services are PostgreSQL-ready and use EF Core migrations.
- Data remains split per service database (`users_db`, `books_db`, `booking_db`).
- In-memory fallback still exists when no connection string is provided.

Relevant files:
- `services/UsersService/Data/*`
- `services/BooksService/Data/*`
- `services/BookingService/Data/*`
- `docker/postgres-init.sql`

---

## How to Inspect Jaeger, RabbitMQ, and PostgreSQL Objects

### Jaeger traces

1. Start the stack:

```bash
make up
```

2. Open Jaeger UI:
- `http://localhost:16686`

3. In Jaeger UI:
- Choose service (for example `frontend`, `booking-service`, `workflow-saga`)
- Click **Find Traces**
- Open a trace to inspect spans and cross-service flow

### RabbitMQ objects (queues, exchanges, bindings)

1. Open RabbitMQ Management UI:
- `http://localhost:15672`
- default credentials: `guest` / `guest`

2. Inspect objects:
- **Queues** tab: message queues created by MassTransit consumers
- **Exchanges** tab: published event exchanges
- **Bindings** tab: routing between exchanges and queues

3. Optional CLI inspection:

```bash
docker compose exec rabbitmq rabbitmqctl list_queues

docker compose exec rabbitmq rabbitmqctl list_exchanges

docker compose exec rabbitmq rabbitmqctl list_bindings
```

### PostgreSQL objects (databases, tables, rows)

1. Connect with `psql` inside container:

```bash
docker compose exec postgres psql -U library -d booking_db
```

2. Useful SQL commands:

```sql
\l
\dt
SELECT * FROM "CartItems";
SELECT * FROM "BookInventories";
```

3. Check other service databases:

```bash
docker compose exec postgres psql -U library -d users_db

docker compose exec postgres psql -U library -d books_db
```

---

## Notes

- This stage delivers the requested architecture pieces and event flow extensions.
- Tracing and resilience are implemented as a practical foundation and can be hardened further in next stages (for example, full circuit-breaker policy wiring and trace dashboards/alerts).
