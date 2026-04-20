## Plan: Library Microservices Scaffold

Draft architecture and delivery plan: bootstrap a polyglot monorepo around the existing `.NET` solution, add three independent dot core api services (`books`, `users`, `booking`), a dot core web-app frontend, and containerized runtime via `docker-compose` + `make`. Implement borrowing workflow with event-driven saga coordination using `MassTransit` in a dedicated workflow component, while keeping each service initially in-memory with PostgreSQL-ready repository boundaries for later migration.

### Stage 1: Scaffold and Core Setup
1. Restructure workspace into service folders and shared ops files: [docker-compose.yml](docker-compose.yml), [Makefile](Makefile), [README.md](README.md), plus `services/*` and `frontend/*`.
2. Define API contracts and seeded data in [services/users/src](services/users/src), [services/books/src](services/books/src), and [services/booking/src](services/booking/src) for `user1/user2` and `Book1..Book3`.
3. Add saga orchestration service under [workflow-saga](workflow-saga) using `MassTransit`, `RabbitMQ`, and a `BorrowingStateMachine` coordinating add/remove/complete borrowing events.
4. Implement PostgreSQL-ready data layers per service with in-memory default repositories and swappable adapters in [services/users/src/repositories](services/users/src/repositories), [services/books/src/repositories](services/books/src/repositories), and [services/booking/src/repositories](services/booking/src/repositories).
5. Build web pages and routing in [frontend/src](frontend/src): home/login/search/add-to-cart, cart/remove/complete, with calls to `users`, `books`, and `booking` endpoints.
6. Containerize each component independently with per-folder `Dockerfile`s, wire env/config and dependencies in [docker-compose.yml](docker-compose.yml), and expose `make` targets for build/up/down/logs/redeploy-by-service.

### Stage 2: Persistent Microservices
1. Resilience patterns (retries, circuit-breakers). To show it - fail every second time a request is made to the `booking-service` and implement retry logic in the frontend and saga.
2. Distributed tracing (Jaeger)
3. Saga placement choice for review: dedicated `.NET` `workflow-saga` service 
4. Each book in the booking service should have a `stock` property (by default 10). When a user adds a book to the cart, the saga should check if the book is in stock before confirming the addition. If the book is out of stock, the saga should publish an event indicating the failure, and the frontend should display an appropriate message to the user. The stock substracted when the cart with this selected book is completed. If the cart is removed, the stock should be added back.

### Stage 3: Transition from RabbitMQ to Kafka (Simple-first)
1. Add Kafka runtime in Docker with the simplest topology first: single-node KRaft broker (no ZooKeeper) and optional Kafka UI for topic inspection.
2. Keep existing message contracts in [shared/Library.Contracts/Class1.cs](shared/Library.Contracts/Class1.cs) unchanged and migrate transport only.
3. Introduce a transport toggle (`Messaging:Provider = RabbitMq|Kafka`) in `booking-service` and `workflow-saga`, defaulting to RabbitMQ until Kafka path is validated.
4. Migrate one flow first (`AddToCartRequested -> CartItemAdded/AddToCartFailed`) and verify end-to-end behavior before moving remove/complete flow.
5. Add Kafka topic conventions: `<domain>.<message-name>`, partition key by `UserId`, and consumer group names per service.
6. Update observability and operations docs with Kafka-specific checks (topic lag, reprocessing, poison message handling).

### Stage 3 Options and Learning Path
1. **Option A (recommended to learn): KRaft single broker + staged migration**
   - Simplest setup and easiest to debug locally.
   - Good for learning topic/partition/consumer-group behavior before clustering concerns.
2. **Option B: ZooKeeper-based Kafka stack**
   - Useful only for legacy compatibility learning.
   - More moving parts and more operational overhead.
3. **Option C: Redpanda local runtime**
   - Fast local startup and low resource usage.
   - Great for developer experience, but less direct exposure to Apache Kafka internals.

### Stage 3 Hardening Scope
1. Implement outbox/inbox idempotency in `booking-service` to handle duplicate delivery safely.
2. Move saga state from in-memory repository to durable persistence (PostgreSQL).
3. Add dead-letter strategy and replay tooling for failed event handling.
4. Add contract and integration tests for event versioning and backward compatibility.

---

### Stage 4: API and schema versioning
#### Learning Focus
- **API versioning** teaches backward compatibility discipline — essential when multiple clients consume the same API at different cadences.
- **API versioning** teaches backward compatibility discipline — essential when multiple clients consume the same API at different cadences.
#### 4.1 API Versioning
1. Introduce URL-segment versioning (`/api/v1/`, `/api/v2/`) in `books-service`, `users-service`, and `booking-service` using `Asp.Versioning.Http` (`Microsoft.AspNetCore.Mvc.Versioning`).
2. Annotate controllers with `[ApiVersion]` and expose version metadata in Swagger/OpenAPI (`Asp.Versioning.ApiExplorer`).
3. Add a deprecation policy: deprecated versions return `Sunset` and `Deprecation` response headers so clients have a migration timeline.
4. Version the Kafka message contracts alongside the HTTP API — use an envelope wrapper `{ schemaVersion, payload }` in `Library.Contracts` so consumers can detect and handle version mismatches at runtime.
#### 4.2 Protobuf / Schema Registry
1. Replace JSON serialization on Kafka topics with **Protocol Buffers** (`Google.Protobuf`) — define `.proto` files for all message types in `shared/Library.Contracts/Proto/`.
2. Register schemas in a **Confluent Schema Registry** container (add to `docker-compose.kafka.yml`) and configure MassTransit Kafka Rider to use `ISchemaRegistryClient` for schema-aware serialization.
3. Enforce schema compatibility mode (`FULL_TRANSITIVE`) on the registry so breaking changes are caught before deployment.
4. Add a `make schema-check` target that runs `kafka-schema-registry-maven-plugin` (or equivalent CLI) to diff local `.proto` files against the registry.

### Stage 5: Security - Authentication and Authorization
#### Learning Focus
- **JWT + YARP gateway** teaches the security perimeter pattern — one trust boundary, token propagation, and policy enforcement in one place.
1. Add an **Identity / OAuth2 server** container (Keycloak or `duende/identityserver`) to `docker-compose.yml`.
2. Protect all service APIs with JWT bearer validation — add `AddAuthentication().AddJwtBearer(...)` and `[Authorize]` to controllers.
3. Implement role-based policies: `librarian` role can manage books/stock; `member` role can browse and borrow.
4. Propagate the JWT access token from the frontend through service-to-service calls and into Kafka message headers for audit trail.

### Stage 6: API Gateway and Kubernetes Deployment
#### Learning Focus
- **Kubernetes** teaches immutable infrastructure and operational runbooks — how the stack behaves when pods restart, scale, or get evicted.
- **CI/CD** ties everything together — no manual steps between a merged PR and a running deployment.
#### 6.1 API Gateway
1. Add a **YARP reverse proxy** service (`Microsoft.ReverseProxy`) as the single ingress point in front of `books-service`, `users-service`, and `booking-service`.
2. Configure route transforms, rate limiting (`System.Threading.RateLimiting`), and request correlation-ID injection at the gateway level.
3. Expose one OpenAPI aggregate spec from the gateway so the frontend and external consumers have a single discovery endpoint.

#### 6.2 Structured Logging and Centralized Log Aggregation
1. Replace plain console logging with **Serilog** (`Serilog.AspNetCore`) across all services — output structured JSON to stdout.
2. Add an **OpenSearch** (or Loki + Grafana) container and configure a Serilog sink to ship logs to it.
3. Inject `TraceId`, `SpanId`, `UserId`, and `CorrelationId` as log properties automatically via middleware so every log entry is linkable to a Jaeger trace.
4. Add a `make logs-search` target and document log query recipes (e.g., find all events for a given `CorrelationId`).

#### 6.3 Health Checks and Readiness Probes
1. Add `AddHealthChecks()` in each service with checks for PostgreSQL, RabbitMQ/Kafka, and upstream HTTP dependencies.
2. Expose `/health/live` (liveness) and `/health/ready` (readiness) endpoints.
3. Wire readiness probes into `docker-compose.yml` `healthcheck` directives so dependent services wait for dependencies to be ready before starting.
4. Add a `make health` target that polls all `/health/ready` endpoints and summarises the stack status.

#### 6.4 Kubernetes Deployment (Local with kind / minikube)
1. Add `k8s/` folder with Kubernetes manifests (Deployments, Services, ConfigMaps, Secrets) for each service.
2. Use **Kustomize** overlays for `dev` vs `prod` environments (resource limits, replica counts, image tags).
3. Add a `make k8s-up` / `make k8s-down` target using `kind` to spin up a local cluster and apply manifests.
4. Move secrets (DB passwords, JWT signing key) to Kubernetes Secrets and demonstrate mounting them as env vars — no secrets in `appsettings.json`.

#### 6.5 CI/CD Pipeline
1. Add a **GitHub Actions** workflow (`.github/workflows/ci.yml`) that:
   - Restores, builds, and runs tests on every PR.
   - Builds and pushes Docker images to GitHub Container Registry (`ghcr.io`) on merge to `main`.
   - Runs schema compatibility check against the Schema Registry.
2. Add a **release workflow** that tags images with SemVer and updates the `k8s/` image tags automatically.

### Stage 7: Testing and Hardening - Contract and Integration Tests
1. Add an `tests/` folder with:
   - **xUnit** integration tests for each HTTP API using `WebApplicationFactory<Program>` with a real PostgreSQL container (`Testcontainers.PostgreSql`).
   - **MassTransit test harness** unit tests for saga state machine transitions.
   - **Pact** consumer-driven contract tests between frontend and `booking-service`.
2. Add Kafka consumer group tests using `Testcontainers.Kafka` to verify end-to-end message flow at the transport layer.

