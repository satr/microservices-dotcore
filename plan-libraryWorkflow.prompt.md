## Plan: Library Microservices Scaffold

Draft architecture and delivery plan: bootstrap a polyglot monorepo around the existing `.NET` solution, add three independent dot core api services (`books`, `users`, `booking`), a dot core web-app frontend, and containerized runtime via `docker-compose` + `make`. Implement borrowing workflow with event-driven saga coordination using `MassTransit` in a dedicated workflow component, while keeping each service initially in-memory with PostgreSQL-ready repository boundaries for later migration.

### Steps
1. Restructure workspace into service folders and shared ops files: [docker-compose.yml](docker-compose.yml), [Makefile](Makefile), [README.md](README.md), plus `services/*` and `frontend/*`.
2. Define API contracts and seeded data in [services/users/src](services/users/src), [services/books/src](services/books/src), and [services/booking/src](services/booking/src) for `user1/user2` and `Book1..Book3`.
3. Add saga orchestration service under [workflow-saga](workflow-saga) using `MassTransit`, `RabbitMQ`, and a `BorrowingStateMachine` coordinating add/remove/complete borrowing events.
4. Implement PostgreSQL-ready data layers per service with in-memory default repositories and swappable adapters in [services/users/src/repositories](services/users/src/repositories), [services/books/src/repositories](services/books/src/repositories), and [services/booking/src/repositories](services/booking/src/repositories).
5. Build web pages and routing in [frontend/src](frontend/src): home/login/search/add-to-cart, cart/remove/complete, with calls to `users`, `books`, and `booking` endpoints.
6. Containerize each component independently with per-folder `Dockerfile`s, wire env/config and dependencies in [docker-compose.yml](docker-compose.yml), and expose `make` targets for build/up/down/logs/redeploy-by-service.

### Further Considerations
1. Saga placement choice for review: Option A dedicated `.NET` `workflow-saga` service (recommended), Option B orchestrate in `booking`, Option C choreography only.
2. PostgreSQL rollout mode: Option A containers now but unused by default, Option B feature-flag per service, Option C defer DB containers initially.
3. If this draft matches your intent, confirm and I will refine it into a task-by-task execution backlog with file-level checkpoints.

