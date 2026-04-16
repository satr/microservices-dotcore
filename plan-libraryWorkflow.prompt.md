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