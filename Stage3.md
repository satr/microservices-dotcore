# Stage 3: Transition from RabbitMQ to Kafka (Simple-first)

## Scope Covered

This stage delivers the Kafka transport migration for `booking-service` and `workflow-saga`, keeping RabbitMQ as the default until the Kafka path is fully validated.

1. Kafka runtime in Docker (KRaft single-node, no ZooKeeper) + Kafka UI
2. Message contracts unchanged — transport layer migrated only
3. Transport toggle `Messaging:Provider = RabbitMq|Kafka` in both services
4. All flows migrated under the toggle (AddToCart, RemoveFromCart, CompleteBorrowing)
5. Kafka topic conventions: `library.<message-name>`, 3 partitions, consumer groups per service
6. Observability and operations docs updated with Kafka-specific checks

---

## What Was Implemented

### 1) Kafka runtime in Docker

- Single-node KRaft broker (`confluentinc/cp-kafka:7.7.1`) — no ZooKeeper needed.
- `kafka-init` one-shot container pre-creates all 7 topics with 3 partitions each on stack startup.
- Kafka UI (`kafbat/kafka-ui:v1.0.0`) at `http://localhost:8081` for topic/consumer-group inspection.
- All Kafka services live in `docker-compose.kafka.yml` (overlay on top of `docker-compose.yml`).

Relevant files:
- `docker-compose.kafka.yml`

Topics created:
```
library.add-to-cart-requested
library.remove-from-cart-requested
library.complete-borrowing-requested
library.cart-item-added
library.cart-item-removed
library.borrowing-completed
library.add-to-cart-failed
```

---

### 2) Contracts unchanged

- `shared/Library.Contracts/Class1.cs` — no changes to message record types.
- Only the MassTransit wiring (producers/consumers/topic endpoints) was updated in service `Program.cs` files.

---

### 3) Transport toggle

Both `booking-service` and `workflow-saga` read `Messaging:Provider` from config:

```json
{
  "Messaging": { "Provider": "RabbitMq" },
  "Kafka": {
    "BootstrapServers": "kafka:9092",
    "TopicPrefix": "library"
  }
}
```

- `RabbitMq` (default) — existing MassTransit RabbitMQ bus, no changes to behavior.
- `Kafka` — MassTransit Rider with `UsingKafka`, InMemory bus for internal pipeline.

`docker-compose.kafka.yml` overrides `booking-service` and `workflow-saga` environment with `Messaging__Provider=Kafka` when the Kafka compose overlay is used.

Relevant files:
- `services/BookingService/appsettings.json`
- `workflow-saga/WorkflowSaga/appsettings.json`
- `services/BookingService/Program.cs`
- `workflow-saga/WorkflowSaga/Program.cs`

---

### 4) Transport abstraction layer

Two transport-agnostic publisher interfaces were introduced so controllers and the saga are not coupled to MassTransit transport type:

- `ICartCommandPublisher` (booking-service) — `PublishAddToCartRequested`, `PublishRemoveFromCartRequested`, `PublishCompleteBorrowingRequested`
  - `RabbitMqCartCommandPublisher` wraps `IPublishEndpoint`
  - `KafkaCartCommandPublisher` wraps three `ITopicProducer<T>`

- `IBorrowingEventPublisher` (workflow-saga) — outcome events back to booking-service
  - `RabbitMqBorrowingEventPublisher` / `KafkaBorrowingEventPublisher`

Relevant files:
- `services/BookingService/Messaging/ICartCommandPublisher.cs`
- `workflow-saga/WorkflowSaga/Saga/IBorrowingEventPublisher.cs`

---

### 5) Kafka topic conventions

| Convention | Value |
|---|---|
| Topic prefix | `library` |
| Topic name format | `library.<message-name-kebab>` |
| Partitions | 3 |
| Replication factor | 1 (single-node dev) |
| Consumer group — booking-service | `booking-service` |
| Consumer group — workflow-saga | `workflow-saga` |
| Offset reset | `Earliest` |

---

### 6) Makefile targets

```bash
# Start Kafka + Kafka UI only (no service rebuild)
make kafka-up

# Start full stack with Kafka transport enabled (booking-service + workflow-saga in Kafka mode)
make kafka-stack-up

# Rebuild and hot-swap only booking-service + workflow-saga in Kafka mode
make kafka-stack-restart

# Tail Kafka logs
make kafka-logs

# Status of Kafka containers
make kafka-ps

# Stop and remove Kafka containers
make kafka-down
```

---

## How to Inspect Kafka Topics, Consumer Groups, and Lag

### Kafka UI (recommended)

1. Start the stack:
```bash
make kafka-stack-up
```

2. Open Kafka UI: `http://localhost:8081`

3. Inspect:
   - **Topics** tab — message count, partition distribution, offsets
   - **Consumer Groups** tab — lag per partition for `booking-service` and `workflow-saga`
   - **Messages** tab per topic — replay individual messages with offset seek

### CLI inspection

```bash
# List topics
docker compose -f docker-compose.yml -f docker-compose.kafka.yml exec kafka \
  kafka-topics --bootstrap-server kafka:9092 --list

# Describe a topic (partitions, replicas, leaders)
docker compose -f docker-compose.yml -f docker-compose.kafka.yml exec kafka \
  kafka-topics --bootstrap-server kafka:9092 --describe --topic library.add-to-cart-requested

# Consumer group lag
docker compose -f docker-compose.yml -f docker-compose.kafka.yml exec kafka \
  kafka-consumer-groups --bootstrap-server kafka:9092 \
  --describe --group booking-service

docker compose -f docker-compose.yml -f docker-compose.kafka.yml exec kafka \
  kafka-consumer-groups --bootstrap-server kafka:9092 \
  --describe --group workflow-saga

# Consume messages from a topic (earliest offset)
docker compose -f docker-compose.yml -f docker-compose.kafka.yml exec kafka \
  kafka-console-consumer --bootstrap-server kafka:9092 \
  --topic library.add-to-cart-requested --from-beginning --max-messages 10
```

### Poison message handling (Stage 3 baseline)

MassTransit Kafka Rider skips and logs messages that fail deserialization. For application-level failures, the consumer throws and MassTransit retries up to the configured retry policy. Messages that exhaust retries are currently logged and skipped — a dead-letter topic strategy is planned for Stage 3 Hardening.

### Reprocessing (offset reset)

To reprocess all messages from the beginning for a consumer group:

```bash
# Stop services first, then reset offsets
docker compose -f docker-compose.yml -f docker-compose.kafka.yml stop booking-service workflow-saga

docker compose -f docker-compose.yml -f docker-compose.kafka.yml exec kafka \
  kafka-consumer-groups --bootstrap-server kafka:9092 \
  --group workflow-saga --reset-offsets --to-earliest --all-topics --execute

docker compose -f docker-compose.yml -f docker-compose.kafka.yml start booking-service workflow-saga
```

---

## Switching Between RabbitMQ and Kafka Modes

| Mode | Command |
|---|---|
| RabbitMQ (default) | `make up` |
| Kafka | `make kafka-stack-up` |
| Kafka — rebuild services only | `make kafka-stack-restart` |

The two modes share PostgreSQL, Jaeger, and the frontend. Only `booking-service` and `workflow-saga` switch transport.

---

## Notes

- Stage 3 Hardening items (outbox/inbox idempotency, durable saga state in PostgreSQL, dead-letter topics, contract tests) are defined in the plan and are out of scope for this stage.
- The `MassTransit.Kafka` package (`8.3.5`) was added to `BookingService` and `WorkflowSaga`.
- The `workflow-saga` Kafka Rider registers a second `BorrowingStateMachine` instance scoped to the Kafka consumer — this is the MassTransit Rider pattern requirement.

