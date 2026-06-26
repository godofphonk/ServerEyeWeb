[![Codecov](https://codecov.io/gh/godofphonk/ServerEyeWeb/branch/master/graph/badge.svg)](https://app.codecov.io/gh/godofphonk/ServerEyeWeb)

# ServerEye Web

 full-stack SaaS платформа для мониторинга серверов: биллинг, тикет-система, OAuth2. clean architecture, полноценная observability и CI/CD.

## Что умеет

- **Мониторинг серверов** — отображение метрик (CPU/RAM/Disk/Network).
- **Аутентификация** — JWT + refresh, email-верификация, восстановление пароля, OAuth (Google, Github, Telegram).
- **Биллинг** — подписки и платежи через **Stripe** и **YooKassa**, вебхуки.
- **Тикеты / поддержка** — отдельная БД и доменная модель.
- **Нотификации** — email-шаблоны (AWS SES).
- **Админка**, профиль, страницы pricing/terms/privacy/docs.

## Стек

### Backend (`backend/ServerEyeBackend`)
- **.NET 10 / ASP.NET Core** — Web API, Minimal hosting + Controllers.
- **Clean Architecture**: `ServerEye.API` → `ServerEye.Infrastructure` → `ServerEye.Core`.
- **EF Core 10** + **PostgreSQL 16** (Npgsql), две БД: основная и тикетная (+ billing).
- **Redis 8** — кэш и distributed cache (`StackExchange.Redis`).
- **FluentValidation**, **BCrypt**, **JWT Bearer**, Google/Github/Telegram OAuth.
- **Stripe.net**, YooKassa, **AWS SES** для email.
- **OpenTelemetry** — трейсы/метрики (OTLP + Prometheus exporter), инструментирование AspNetCore/HttpClient/EF/Redis.
- **HealthChecks** для Postgres/Redis.
- **StyleCop.Analyzers**, централизованные версии пакетов (`Directory.Packages.props`).
- Тесты: **xUnit**, **FluentAssertions**, **Moq/NSubstitute**, **AutoFixture**, **Testcontainers** для интеграционных тестов на реальном Postgres, **Coverlet** + Codecov.

### Frontend (`frontend`)
- **Next.js 16** (App Router), **React 19**, **TypeScript 6**.
- **TailwindCSS 4**, **Framer Motion**, **Recharts**, **Lucide**.
- **Axios**, кастомный proxy-слой (`proxy.ts`).
- **OpenTelemetry Web SDK** — клиентские трейсы.
- Тесты: **Jest** + **Testing Library**, e2e через **Playwright** (auth, oauth, payment, metrics, tickets, profile, install и т.д.).
- **ESLint**, **Prettier**, **Lighthouse CI**.
- Управление секретами через **Doppler**.

### Infrastructure / DevOps
- **Docker Compose** — отдельные стеки: `infrastructure` (Postgres × 3, Redis), `observability`, `backend`, `frontend`, `stripe` (CLI для прокидывания вебхуков). Раздельно для `dev/` и `prod/` (`environments/`).
- **Observability**: **Grafana + Prometheus + Loki + Tempo + Grafana Alloy**, готовые дашборды в `environments/dev/observability/dashboards`.
- **Makefile** — единая точка входа: `make dev-up / dev-down / build / test / lint / prod-up`, профилирование (`prof-memory`, `prof-cpu` через dotTrace).
- **GitHub Actions**: backend-ci, frontend-ci, общий ci, **CodeQL**, deploy-production, rollback-production.
- **Dependabot**, **markdown-link-check**, лейблер.
- **Профилирование**: dotTrace + memory snapshots в `profiling/`.

## Структура репозитория

```
backend/ServerEyeBackend/   # .NET-решение (API / Core / Infrastructure / UnitTests / IntegrationTests)
frontend/                    # Next.js приложение (app router, e2e, hooks, lib, components)
environments/                # docker-compose стеки для dev и prod (infra, observability, stripe и т.д.)
profiling/                   # сценарии нагрузки и снапшоты памяти/CPU
.github/workflows/           # CI/CD pipelines + CodeQL
Makefile                     # оркестрация всех окружений
```

