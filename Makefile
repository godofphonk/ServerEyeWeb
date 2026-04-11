# ServerEye Web - Environment Management Makefile
# Enterprise-level environment management with component separation

.PHONY: help dev-up dev-down dev-logs dev-clean build test lint dev-infra-up dev-observability-up dev-backend-up dev-frontend-up dev-stripe-up dev-infra-down dev-observability-down dev-backend-down dev-frontend-down dev-stripe-down dev-infra-logs dev-observability-logs dev-backend-logs dev-frontend-logs dev-stripe-logs dev-shell

# Default target
help:
	@echo "🚀 ServerEye Web - Enterprise Environment Management"
	@echo ""
	@echo "📦 Development - Full Stack:"
	@echo "  dev-up           - Start all development services (infra + stripe + observability + backend + frontend)"
	@echo "  dev-down         - Stop all development services"
	@echo "  dev-logs         - Show all development logs"
	@echo "  dev-clean        - Clean all development services"
	@echo "  dev-restart      - Restart all development services"
	@echo ""
	@echo "🔧 Development - Component-wise:"
	@echo "  dev-infra-up     - Start infrastructure only (PostgreSQL, Redis, Mock API)"
	@echo "  dev-stripe-up   - Start Stripe CLI for webhook forwarding"
	@echo "  dev-observability-up - Start observability stack (Grafana, Prometheus, Loki)"
	@echo "  dev-backend-up   - Start backend only (requires infrastructure)"
	@echo "  dev-frontend-up  - Start frontend only (requires backend)"
	@echo "  dev-infra-down   - Stop infrastructure"
	@echo "  dev-stripe-down  - Stop Stripe CLI"
	@echo "  dev-observability-down - Stop observability stack"
	@echo "  dev-backend-down - Stop backend"
	@echo "  dev-frontend-down - Stop frontend"
	@echo "  dev-infra-logs   - Show infrastructure logs"
	@echo "  dev-stripe-logs  - Show Stripe CLI logs"
	@echo "  dev-observability-logs - Show observability stack logs"
	@echo "  dev-backend-logs - Show backend logs"
	@echo "  dev-frontend-logs - Show frontend logs"
	@echo ""
	@echo "� Build & Test:"
	@echo "  build            - Build all development services"
	@echo "  test             - Run all tests (including Docker tests)"
	@echo "  test-backend     - Run all backend tests"
	@echo "  test-backend-ci  - Run backend CI/CD tests (no Docker)"
	@echo "  test-frontend    - Run frontend tests"
	@echo "  lint             - Run linting"
	@echo ""
	@echo "🛠️  Utility:"
	@echo "  clean            - Clean all environments"
	@echo "  status           - Show status of all services"
	@echo "  backup           - Backup databases"

# ==============================================================================
# DEVELOPMENT ENVIRONMENT
# ==============================================================================

# Full Stack Development
dev-up: dev-infra-up dev-stripe-up dev-observability-up dev-backend-up dev-frontend-up
	@echo "https://stripe.com/docs/webhooks"
	@echo "All development services started!"
	@echo "Frontend: http://127.0.0.1:3000"
	@echo "Backend:  http://127.0.0.1:5246"
	@echo "PostgreSQL: 127.0.0.1:5433 (main), 127.0.0.1:5434 (tickets)"
	@echo "Redis: 127.0.0.1:6380"
	@echo "Grafana: http://localhost:3010 (admin/admin)"
	@echo "Prometheus: http://localhost:9090"
	@echo "Loki: http://localhost:3100"
	@echo "Tempo: http://localhost:3200"

dev-down: dev-frontend-down dev-backend-down dev-stripe-down dev-observability-down dev-infra-down
	@echo "✅ All development services stopped!"

dev-logs:
	@echo "Showing all development logs..."
	docker compose -f ./environments/dev/infrastructure/docker-compose.yml logs -f & \
	docker compose -f ./environments/dev/stripe/docker-compose.yml logs -f & \
	docker compose -f ./environments/dev/observability/docker-compose.yml logs -f & \
	docker compose -f ./environments/dev/backend/docker-compose.yml logs -f & \
	docker compose -f ./environments/dev/frontend/docker-compose.yml logs -f

dev-clean: dev-down
	@echo "Cleaning development environment..."
	docker compose -f ./environments/dev/infrastructure/docker-compose.yml down -v --remove-orphans
	docker compose -f ./environments/dev/stripe/docker-compose.yml down -v --remove-orphans
	docker compose -f ./environments/dev/observability/docker-compose.yml down -v --remove-orphans
	docker compose -f ./environments/dev/backend/docker-compose.yml down -v --remove-orphans
	docker compose -f ./environments/dev/frontend/docker-compose.yml down -v --remove-orphans
	docker system prune -f
	@echo "Development environment cleaned!"

dev-restart: dev-down dev-up

# Component-wise Development Commands
dev-infra-up:
	@echo "🏗️  Starting development infrastructure..."
	@docker network create servereye-network 2>/dev/null || echo "✅ Network servereye-network already exists"
	cd ./environments/dev && docker compose -f ./infrastructure/docker-compose.yml --env-file .env up -d
	@echo "✅ Infrastructure started!"

dev-observability-up:
	@echo "📊 Starting observability stack..."
	@docker network create servereye-network 2>/dev/null || echo "✅ Network servereye-network already exists"
	docker compose -f ./environments/dev/observability/docker-compose.yml up -d
	@echo "✅ Observability stack started!"
	@echo "📊 Grafana: http://localhost:3010 (admin/admin)"
	@echo "📈 Prometheus: http://localhost:9090"
	@echo "📋 Loki: http://localhost:3100"
	@echo "🔍 Tempo: http://localhost:3200"
	@echo "🔧 Alloy: http://localhost:12345"

dev-backend-up:
	@echo "🔧 Starting development backend..."
	@docker network create servereye-network 2>/dev/null || echo "✅ Network servereye-network already exists"
	cd ./environments/dev && docker compose -f ./backend/docker-compose.yml --env-file .env up -d --build
	@echo "✅ Backend started!"

dev-frontend-up:
	@echo "🌐 Starting development frontend..."
	@docker network create servereye-network 2>/dev/null || echo "Network servereye-network already exists"
	cd ./environments/dev && docker compose -f ./frontend/docker-compose.yml --env-file .env up -d --build
	@echo "Frontend started!"

dev-stripe-up:
	@echo "Starting Stripe CLI for webhook forwarding..."
	@docker network create servereye-network 2>/dev/null || echo "Network servereye-network already exists"
	cd ./environments/dev && docker compose -f ./stripe/docker-compose.yml --env-file .env up -d
	@echo "Stripe CLI started!"
	@echo "Webhooks forwarding to: http://servereye-backend-dev:8080/api/webhooks/stripe"

dev-infra-down:
	@echo "🛑 Stopping infrastructure..."
	docker compose -f ./environments/dev/infrastructure/docker-compose.yml down

dev-observability-down:
	@echo "🛑 Stopping observability stack..."
	docker compose -f ./environments/dev/observability/docker-compose.yml down

dev-backend-down:
	@echo "🛑 Stopping backend..."
	docker compose -f ./environments/dev/backend/docker-compose.yml down

dev-frontend-down:
	@echo "🛑 Stopping frontend..."
	docker compose -f ./environments/dev/frontend/docker-compose.yml down

dev-stripe-down:
	@echo "🛑 Stopping Stripe CLI..."
	docker compose -f ./environments/dev/stripe/docker-compose.yml down

dev-infra-logs:
	@echo "📋 Infrastructure logs..."
	docker compose -f ./environments/dev/infrastructure/docker-compose.yml logs -f

dev-observability-logs:
	@echo "📋 Observability stack logs..."
	docker compose -f ./environments/dev/observability/docker-compose.yml logs -f

dev-backend-logs:
	@echo "📋 Backend logs..."
	docker compose -f ./environments/dev/backend/docker-compose.yml logs -f

dev-frontend-logs:
	@echo "📋 Frontend logs..."
	docker compose -f ./environments/dev/frontend/docker-compose.yml logs -f

dev-stripe-logs:
	@echo "📋 Stripe CLI logs..."
	docker compose -f ./environments/dev/stripe/docker-compose.yml logs -f

dev-shell:
	@echo "🐚 Accessing development backend shell..."
	docker exec -it servereye-backend-dev /bin/sh

# ==============================================================================
# BUILD COMMANDS
# ==============================================================================

build:
	@echo "� Building all development services..."
	make build-dev
	@echo "✅ All development services built!"

build-dev:
	@echo "🔨 Building development services..."
	docker compose -f ./environments/dev/infrastructure/docker-compose.yml build --no-cache
	docker compose -f ./environments/dev/backend/docker-compose.yml build --no-cache
	docker compose -f ./environments/dev/frontend/docker-compose.yml build --no-cache
	@echo "✅ Development services built!"

# ==============================================================================
# TEST COMMANDS
# ==============================================================================

test:
	@echo "🧪 Running all tests..."
	make test-backend
	make test-frontend
	@echo "✅ All tests completed!"

test-backend:
	@echo "🧪 Running backend tests..."
	cd ./backend/ServerEyeBackend && dotnet test --logger "console;verbosity=detailed"
	@echo "✅ Backend tests completed!"

test-backend-ci:
	@echo "🧪 Running backend CI/CD tests (Unit + Integration)..."
	cd ./backend/ServerEyeBackend && \
		echo "📝 Unit Tests..." && \
		dotnet test ServerEye.UnitTests --logger "console;verbosity=minimal" --no-build && \
		echo "🔧 Integration Tests..." && \
		dotnet test ServerEye.IntegrationTests --logger "console;verbosity=minimal" --no-build
	@echo "✅ Backend CI/CD tests completed!"

test-frontend:
	@echo "🧪 Running frontend tests..."
	cd ./frontend && npm test -- --coverage --watchAll=false
	@echo "✅ Frontend tests completed!"

# ==============================================================================
# LINT COMMANDS
# ==============================================================================

lint:
	@echo "🔍 Running linting for all services..."
	make lint-backend
	make lint-frontend
	@echo "✅ Linting completed!"

lint-backend:
	@echo "🔍 Running backend linting..."
	cd ./backend/ServerEyeBackend && dotnet format --verify-no-changes
	@echo "✅ Backend linting completed!"

lint-frontend:
	@echo "🔍 Running frontend linting..."
	cd ./frontend && npm run lint
	@echo "✅ Frontend linting completed!"

# ==============================================================================
# UTILITY COMMANDS
# ==============================================================================

clean:
	@echo "🧹 Cleaning all environments..."
	make dev-clean
	docker system prune -af --volumes
	@echo "✅ All environments cleaned!"

status:
	@echo "📊 Status of all development environments..."
	@echo ""
	@echo "🔧 Development:"
	@echo "Infrastructure:"
	@docker compose -f ./environments/dev/infrastructure/docker-compose.yml ps 2>/dev/null || echo "Infrastructure not running"
	@echo "Stripe:"
	@docker compose -f ./environments/dev/stripe/docker-compose.yml ps 2>/dev/null || echo "Stripe CLI not running"
	@echo "Observability:"
	@docker compose -f ./environments/dev/observability/docker-compose.yml ps 2>/dev/null || echo "Observability stack not running"
	@echo "Backend:"
	@docker compose -f ./environments/dev/backend/docker-compose.yml ps 2>/dev/null || echo "Backend not running"
	@echo "Frontend:"
	@docker compose -f ./environments/dev/frontend/docker-compose.yml ps 2>/dev/null || echo "Frontend not running"

backup:
	@echo "💾 Creating database backups..."
	@mkdir -p ./backups/dev
	@echo "Backing up development databases..."
	docker exec servereyeWeb-postgres pg_dump -U postgres ServerEyeWeb_Dev > ./backups/dev/main_db_$(shell date +%Y%m%d_%H%M%S).sql
	docker exec servereyeWeb-ticket-postgres pg_dump -U postgres ServerEyeWeb_Dev_Ticket > ./backups/dev/ticket_db_$(shell date +%Y%m%d_%H%M%S).sql
	@echo "✅ Development backups completed!"
	@echo "💾 Backups saved in ./backups/dev/"

restore:
	@echo "🔄 Restoring databases..."
	@echo "⚠️  This will overwrite existing databases!"
	@read -p "Are you sure? (y/N): " confirm && [ "$$confirm" = "y" ] || exit 1
	@echo "Please specify backup files:"
	@read -p "Main DB backup: " main_backup && \
	read -p "Ticket DB backup: " ticket_backup && \
	docker exec -i servereyeWeb-postgres psql -U postgres -c "DROP DATABASE IF EXISTS \"ServerEyeWeb_Dev\";" && \
	docker exec -i servereyeWeb-postgres psql -U postgres -c "CREATE DATABASE \"ServerEyeWeb_Dev\";" && \
	docker exec -i servereyeWeb-postgres psql -U postgres ServerEyeWeb_Dev < "$$main_backup" && \
	docker exec -i servereyeWeb-ticket-postgres psql -U postgres -c "DROP DATABASE IF EXISTS \"ServerEyeWeb_Dev_Ticket\";" && \
	docker exec -i servereyeWeb-ticket-postgres psql -U postgres -c "CREATE DATABASE \"ServerEyeWeb_Dev_Ticket\";" && \
	docker exec -i servereyeWeb-ticket-postgres psql -U postgres ServerEyeWeb_Dev_Ticket < "$$ticket_backup"
	@echo "✅ Databases restored!"

# ==============================================================================
# DEVELOPMENT HELPERS
# ==============================================================================

dev-migrate:
	@echo "🗄️  Running database migrations..."
	docker exec servereyeWeb-backend dotnet ef database update --project ServerEye.Infrastructure --startup-project ServerEye.API
	@echo "✅ Migrations completed!"

dev-seed:
	@echo "🌱 Seeding development data..."
	docker exec servereyeWeb-backend dotnet ServerEye.API.dll --seed-data
	@echo "✅ Development data seeded!"

dev-reset:
	@echo "🔄 Resetting development environment..."
	make dev-clean
	docker volume rm servereye_postgres_dev_data servereye_postgres_ticket_dev_data 2>/dev/null || true
	make dev-up
	@echo "✅ Development environment reset!"
