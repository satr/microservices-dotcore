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
kafka-up: ## Start Kafka + Kafka UI (Stage 3 scaffold)
	$(COMPOSE_KAFKA) up -d kafka kafka-ui

.PHONY: kafka-down
kafka-down: ## Stop Kafka + Kafka UI and remove their containers
	$(COMPOSE_KAFKA) rm -sf kafka kafka-ui

.PHONY: kafka-logs
kafka-logs: ## Tail Kafka + Kafka UI logs
	$(COMPOSE_KAFKA) logs -f kafka kafka-ui

.PHONY: kafka-ps
kafka-ps: ## Show Kafka + Kafka UI container status
	$(COMPOSE_KAFKA) ps kafka kafka-ui

.PHONY: clean
clean: ## Stop containers and remove volumes
	$(COMPOSE) down -v

