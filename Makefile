# ServerEye Web - Environment Management Makefile
# Enterprise-level environment management with component separation

.PHONY: help dev-up dev-down dev-logs dev-clean prod-setup prod-deploy prod-logs prod-clean build test lint

# Default target
help:
	@echo "🚀 ServerEye Web - Enterprise Environment Management"
	@echo ""
	@echo "📦 Development - Full Stack:"
	@echo "  dev-up           - Start all development services (infra + backend + frontend)"
	@echo "  dev-down         - Stop all development services"
	@echo "  dev-logs         - Show all development logs"
	@echo "  dev-clean        - Clean all development services"
	@echo "  dev-restart      - Restart all development services"
	@echo ""
	@echo "🔧 Development - Component-wise:"
	@echo "  dev-infra-up     - Start infrastructure only (PostgreSQL, Redis, Mock API)"
	@echo "  dev-backend-up   - Start backend only (requires infrastructure)"
	@echo "  dev-frontend-up  - Start frontend only (requires backend)"
	@echo "  dev-infra-down   - Stop infrastructure"
	@echo "  dev-backend-down - Stop backend"
	@echo "  dev-frontend-down - Stop frontend"
	@echo "  dev-infra-logs   - Show infrastructure logs"
	@echo "  dev-backend-logs - Show backend logs"
	@echo "  dev-frontend-logs - Show frontend logs"
	@echo ""
	@echo "🚀 Production - Full Stack:"
	@echo "  prod-setup       - Setup production environment (Doppler)"
	@echo "  prod-deploy      - Deploy all production services"
	@echo "  prod-down        - Stop all production services"
	@echo "  prod-logs        - Show all production logs"
	@echo "  prod-clean       - Clean all production services"
	@echo "  prod-status      - Check production status"
	@echo ""
	@echo "🏭 Production - Component-wise:"
	@echo "  prod-infra-up    - Start production infrastructure"
	@echo "  prod-backend-up  - Start production backend"
	@echo "  prod-frontend-up - Start production frontend"
	@echo "  prod-infra-down  - Stop production infrastructure"
	@echo "  prod-backend-down - Stop production backend"
	@echo "  prod-frontend-down - Stop production frontend"
	@echo ""
	@echo "🔨 Build & Test:"
	@echo "  build            - Build all services"
	@echo "  test             - Run all tests (including Docker tests)"
	@echo "  test-ci          - Run CI/CD tests (Unit + Simple Integration only)"
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
dev-up: dev-infra-up dev-backend-up dev-frontend-up
	@echo "✅ All development services started!"
	@echo "🌐 Frontend: http://127.0.0.1:3000"
	@echo "🔧 Backend:  http://127.0.0.1:5246"
	@echo "🗄️  PostgreSQL: 127.0.0.1:5433 (main), 127.0.0.1:5434 (tickets)"
	@echo "🔴 Redis: 127.0.0.1:6380"

dev-down: dev-frontend-down dev-backend-down dev-infra-down
	@echo "✅ All development services stopped!"

dev-logs:
	@echo "� Showing all development logs..."
	docker compose -f ./environments/dev/infrastructure/docker-compose.yml logs -f & \
	docker compose -f ./environments/dev/backend/docker-compose.yml logs -f & \
	docker compose -f ./environments/dev/frontend/docker-compose.yml logs -f

dev-clean: dev-down
	@echo "🧹 Cleaning development environment..."
	docker compose -f ./environments/dev/infrastructure/docker-compose.yml down -v --remove-orphans
	docker compose -f ./environments/dev/backend/docker-compose.yml down -v --remove-orphans
	docker compose -f ./environments/dev/frontend/docker-compose.yml down -v --remove-orphans
	docker system prune -f
	@echo "✅ Development environment cleaned!"

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
	@docker network create servereye-network 2>/dev/null || echo "✅ Network servereye-network already exists"
	cd ./environments/dev && docker compose -f ./frontend/docker-compose.yml --env-file .env up -d --build
	@echo "✅ Frontend started!"

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

dev-shell:
	@echo "🐚 Accessing development backend shell..."
	docker exec -it servereye-backend-dev /bin/sh

# ==============================================================================
# PRODUCTION ENVIRONMENT
# ==============================================================================

prod-setup:
	@echo "⚙️  Setting up production environment..."
	@if [ ! -f "./environments/prod/setup-doppler.sh" ]; then \
		echo "❌ Doppler setup script not found!"; \
		exit 1; \
	fi
	chmod +x ./environments/prod/setup-doppler.sh
	./environments/prod/setup-doppler.sh
	@echo "✅ Production environment setup completed!"

# Full Stack Production
prod-deploy: prod-infra-up prod-backend-up prod-frontend-up
	@echo "✅ All production services deployed!"
	@echo "🌐 Frontend: https://servereye.com"
	@echo "🔧 Backend:  https://api.servereye.com"

prod-down: prod-frontend-down prod-backend-down prod-infra-down
	@echo "✅ All production services stopped!"

prod-logs:
	@echo "📋 Showing all production logs..."
	docker compose -f ./environments/prod/infrastructure/docker-compose.yml logs -f & \
	docker compose -f ./environments/prod/backend/docker-compose.yml logs -f & \
	docker compose -f ./environments/prod/frontend/docker-compose.yml logs -f

prod-clean: prod-down
	@echo "🧹 Cleaning production environment..."
	docker compose -f ./environments/prod/infrastructure/docker-compose.yml down -v --remove-orphans
	docker compose -f ./environments/prod/backend/docker-compose.yml down -v --remove-orphans
	docker compose -f ./environments/prod/frontend/docker-compose.yml down -v --remove-orphans
	@echo "✅ Production environment cleaned!"

prod-status:
	@echo "📊 Checking production status..."
	@echo "\n🏗️  Infrastructure:"
	docker compose -f ./environments/prod/infrastructure/docker-compose.yml ps
	@echo "\n🔧 Backend:"
	docker compose -f ./environments/prod/backend/docker-compose.yml ps
	@echo "\n🌐 Frontend:"
	docker compose -f ./environments/prod/frontend/docker-compose.yml ps

# Component-wise Production Commands
prod-infra-up:
	@echo "🏗️  Starting production infrastructure..."
	@docker network create servereye-network 2>/dev/null || echo "✅ Network servereye-network already exists"
	docker compose -f ./environments/prod/infrastructure/docker-compose.yml up -d
	@echo "✅ Infrastructure started!"

prod-backend-up:
	@echo "🔧 Starting production backend..."
	@docker network create servereye-network 2>/dev/null || echo "✅ Network servereye-network already exists"
	docker compose -f ./environments/prod/backend/docker-compose.yml up -d --build
	@echo "✅ Backend started!"

prod-frontend-up:
	@echo "🌐 Starting production frontend..."
	@docker network create servereye-network 2>/dev/null || echo "✅ Network servereye-network already exists"
	docker compose -f ./environments/prod/frontend/docker-compose.yml up -d --build
	@echo "✅ Frontend started!"

prod-infra-down:
	@echo "🛑 Stopping production infrastructure..."
	docker compose -f ./environments/prod/infrastructure/docker-compose.yml down

prod-backend-down:
	@echo "🛑 Stopping production backend..."
	docker compose -f ./environments/prod/backend/docker-compose.yml down

prod-frontend-down:
	@echo "🛑 Stopping production frontend..."
	docker compose -f ./environments/prod/frontend/docker-compose.yml down

prod-infra-logs:
	@echo "📋 Production infrastructure logs..."
	docker compose -f ./environments/prod/infrastructure/docker-compose.yml logs -f

prod-backend-logs:
	@echo "📋 Production backend logs..."
	docker compose -f ./environments/prod/backend/docker-compose.yml logs -f

prod-frontend-logs:
	@echo "📋 Production frontend logs..."
	docker compose -f ./environments/prod/frontend/docker-compose.yml logs -f

# ==============================================================================
# BUILD COMMANDS
# ==============================================================================

build:
	@echo "🔨 Building all services..."
	make build-dev
	make build-prod
	@echo "✅ All services built!"

build-dev:
	@echo "🔨 Building development services..."
	docker compose -f ./environments/dev/infrastructure/docker-compose.yml build --no-cache
	docker compose -f ./environments/dev/backend/docker-compose.yml build --no-cache
	docker compose -f ./environments/dev/frontend/docker-compose.yml build --no-cache
	@echo "✅ Development services built!"

build-prod:
	@echo "🔨 Building production services..."
	docker build -f ./environments/dockerfiles/backend-prod.Dockerfile -t servereye-backend:prod ./backend/ServerEyeBackend/
	docker build -f ./environments/dockerfiles/frontend-prod.Dockerfile -t servereye-frontend:prod ./frontend/
	@echo "✅ Production services built!"

# ==============================================================================
# TEST COMMANDS
# ==============================================================================

test:
	@echo "🧪 Running all tests..."
	make test-backend
	make test-frontend
	@echo "✅ All tests completed!"

test-ci:
	@echo "🚀 Running CI/CD tests (reliable only)..."
	./ci-tests.sh
	@echo "✅ CI/CD tests completed!"

test-backend:
	@echo "🧪 Running backend tests..."
	cd ./backend/ServerEyeBackend && dotnet test --logger "console;verbosity=detailed"
	@echo "✅ Backend tests completed!"

test-backend-ci:
	@echo "🧪 Running backend CI/CD tests (Unit + Simple Integration)..."
	cd ./backend/ServerEyeBackend && \
		echo "📝 Unit Tests..." && \
		dotnet test ServerEye.UnitTests --logger "console;verbosity=minimal" --no-build && \
		echo "🔧 Simple Integration Tests..." && \
		dotnet test ServerEye.IntegrationTests --filter "FullyQualifiedName~Simple" --logger "console;verbosity=minimal" --no-build
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
	make prod-clean
	docker system prune -af --volumes
	@echo "✅ All environments cleaned!"

status:
	@echo "📊 Status of all environments..."
	@echo ""
	@echo "🔧 Development:"
	@docker compose -f ./environments/dev/docker-compose.yml ps 2>/dev/null || echo "Development environment not running"
	@echo ""
	@echo "🚀 Production:"
	@if command -v doppler &> /dev/null; then \
		doppler run --config=production -- docker compose -f ./environments/prod/docker-compose.yml ps 2>/dev/null || echo "Production environment not running"; \
	else \
		echo "Doppler CLI not installed - run 'make prod-setup'"; \
	fi

backup:
	@echo "💾 Creating database backups..."
	@mkdir -p ./backups/dev
	@mkdir -p ./backups/prod
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
