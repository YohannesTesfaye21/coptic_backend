.PHONY: help build up down restart logs clean test

help: ## Show this help message
	@echo 'Usage: make [target]'
	@echo ''
	@echo 'Targets:'
	@awk 'BEGIN {FS = ":.*?## "} /^[a-zA-Z_-]+:.*?## / {printf "  %-15s %s\n", $$1, $$2}' $(MAKEFILE_LIST)

build: ## Build the Docker images
	docker-compose build

up: ## Start the services
	docker-compose up -d

down: ## Stop the services
	docker-compose down

restart: ## Restart the services
	docker-compose restart

logs: ## Show logs from all services
	docker-compose logs -f

logs-api: ## Show logs from API service
	docker-compose logs -f api

logs-db: ## Show logs from database service
	docker-compose logs -f postgres

clean: ## Remove all containers, networks, and volumes
	docker-compose down -v --remove-orphans
	docker system prune -f

test: ## Run tests
	dotnet test

dev: ## Start development environment
	docker-compose -f docker-compose.yml -f docker-compose.override.yml up -d

prod: ## Start production environment
	docker-compose -f docker-compose.prod.yml up -d

build-prod: ## Build production images
	docker-compose -f docker-compose.prod.yml build

shell: ## Open shell in API container
	docker-compose exec api /bin/bash

db-shell: ## Open shell in database container
	docker-compose exec postgres psql -U coptic_user -d coptic_app
