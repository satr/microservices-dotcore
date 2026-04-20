.DEFAULT_GOAL := help

COMPOSE = docker compose
COMPOSE_KAFKA = docker compose -f docker-compose.yml -f docker-compose.kafka.yml

##@ General

.PHONY: help
help: ## Show this help
	@awk 'BEGIN {FS = ":.*##"; printf "\nUsage:\n  make \033[36m<target>\033[0m\n"} /^[a-zA-Z_-]+:.*?##/ { printf "  \033[36m%-22s\033[0m %s\n", $$1, $$2 } /^##@/ { printf "\n\033[1m%s\033[0m\n", substr($$0, 5) } ' $(MAKEFILE_LIST)

##@ Lifecycle

.PHONY: up
up: ## Start all services (build if needed)
	$(COMPOSE) up -d --build

.PHONY: down
down: ## Stop and remove containers
	$(COMPOSE) down

.PHONY: restart
restart: down up ## Full restart

.PHONY: logs
logs: ## Tail logs from all services
	$(COMPOSE) logs -f

##@ Individual services

.PHONY: build-users
build-users: ## Re-build and restart users-service
	$(COMPOSE) build users-service
	$(COMPOSE) up -d --no-deps users-service

.PHONY: build-books
build-books: ## Re-build and restart books-service
	$(COMPOSE) build books-service
	$(COMPOSE) up -d --no-deps books-service

.PHONY: build-booking
build-booking: ## Re-build and restart booking-service
	$(COMPOSE) build booking-service
	$(COMPOSE) up -d --no-deps booking-service

.PHONY: build-saga
build-saga: ## Re-build and restart workflow-saga
	$(COMPOSE) build workflow-saga
	$(COMPOSE) up -d --no-deps workflow-saga

.PHONY: build-frontend
build-frontend: ## Re-build and restart frontend
	$(COMPOSE) build frontend
	$(COMPOSE) up -d --no-deps frontend

##@ Logging (per service)

.PHONY: logs-users
logs-users: ## Tail users-service logs
	$(COMPOSE) logs -f users-service

.PHONY: logs-books
logs-books: ## Tail books-service logs
	$(COMPOSE) logs -f books-service

.PHONY: logs-booking
logs-booking: ## Tail booking-service logs
	$(COMPOSE) logs -f booking-service

.PHONY: logs-saga
logs-saga: ## Tail workflow-saga logs
	$(COMPOSE) logs -f workflow-saga

.PHONY: logs-frontend
logs-frontend: ## Tail frontend logs
	$(COMPOSE) logs -f frontend

##@ Infrastructure

.PHONY: ps
ps: ## Show running containers
	$(COMPOSE) ps

.PHONY: kafka-up
kafka-up: ## Start Kafka + Schema Registry + Kafka UI (Stage 3/4 scaffold, no service rebuild)
	$(COMPOSE_KAFKA) up -d kafka schema-registry kafka-ui

.PHONY: kafka-stack-up
kafka-stack-up: ## Start full stack with Kafka transport (booking-service + workflow-saga use Kafka)
	$(COMPOSE_KAFKA) up -d --build

.PHONY: kafka-stack-restart
kafka-stack-restart: ## Rebuild and restart booking-service + workflow-saga in Kafka mode
	$(COMPOSE_KAFKA) build booking-service workflow-saga
	$(COMPOSE_KAFKA) up -d --no-deps booking-service workflow-saga

.PHONY: kafka-schema-up
kafka-schema-up: ## Start Kafka + Schema Registry + Kafka UI (Stage 4)
	$(COMPOSE_KAFKA) up -d kafka schema-registry kafka-ui

.PHONY: schema-check
schema-check: ## List registered subjects in Schema Registry and show compatibility settings
	@echo "=== Registered subjects ==="
	@curl -sf http://localhost:8082/subjects | python3 -m json.tool || echo "(no subjects registered yet)"
	@echo ""
	@echo "=== Global compatibility ==="
	@curl -sf http://localhost:8082/config | python3 -m json.tool || true

.PHONY: schema-set-full-transitive
schema-set-full-transitive: ## Enforce FULL_TRANSITIVE compatibility on Schema Registry (breaking changes blocked)
	curl -X PUT http://localhost:8082/config \
	  -H "Content-Type: application/vnd.schemaregistry.v1+json" \
	  -d '{"compatibility":"FULL_TRANSITIVE"}'

.PHONY: kafka-down
kafka-down: ## Stop Kafka + Kafka UI and remove their containers
	$(COMPOSE_KAFKA) rm -sf kafka kafka-ui schema-registry

.PHONY: kafka-logs
kafka-logs: ## Tail Kafka + Schema Registry + Kafka UI logs
	$(COMPOSE_KAFKA) logs -f kafka schema-registry kafka-ui

.PHONY: kafka-ps
kafka-ps: ## Show Kafka + Schema Registry + Kafka UI container status
	$(COMPOSE_KAFKA) ps kafka schema-registry kafka-ui

.PHONY: clean
clean: ## Stop containers and remove volumes
	$(COMPOSE) down -v

