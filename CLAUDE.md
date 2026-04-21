# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Common commands

### Run the stack (development — hot reload)
```bash
docker compose -f docker-compose.yml -f docker-compose.dev.yml up --build
```

### Run the stack (production-like)
```bash
docker compose up --build
```

### Tear down (keep data)
```bash
docker compose down
```

### Tear down (wipe volumes)
```bash
docker compose down -v
```

### Build API only
```bash
dotnet build Cookbook.ApiService/Cookbook.ApiService.csproj
```

### Run all tests
```bash
dotnet test
```

### Run integration tests only
```bash
dotnet test Cookbook.IntegrationTests/Cookbook.IntegrationTests.csproj
```

### Run a single test
```bash
dotnet test Cookbook.IntegrationTests/Cookbook.IntegrationTests.csproj --filter "FullyQualifiedName~TestNameHere"
```

### Lint / format check
```bash
dotnet format Cookbook.ApiService/Cookbook.ApiService.csproj --verify-no-changes
```

### Add a SQL Server EF Core migration
```bash
dotnet ef migrations add <MigrationName> --project Cookbook.ApiService --context CookbookDbContext
```

## Architecture

### Services
- **`Cookbook.ApiService`** — ASP.NET Core 10 minimal API. All endpoints are defined in `Program.cs`. No controllers.
- **`CookbookMauiBlazor.Web`** — Blazor WebAssembly frontend that calls the API.
- **`Cookbook.ServiceDefaults`** — Shared .NET Aspire service defaults (OpenTelemetry wiring, health checks).
- **`CookbookMauiBlazor.Shared`** — Shared DTOs and request/response types used by both API and frontend.
- **`Cookbook.IntegrationTests`** — Integration tests using Testcontainers (spins up a real PostgreSQL container per test run).

### Databases
- **SQL Server** (`CookbookDbContext`) — primary store for recipes, boards, board permissions, user profiles, and data protection keys. EF Core migrations live in `Cookbook.ApiService/Data/Migrations/`. Migrations run automatically on API startup with retry logic.
- **PostgreSQL** (`AuditDbContext`) — audit log store. Schema is created via `EnsureCreatedAsync` on startup (no migration files). Stores `AuditLogEntry` rows written by the create-board, create-recipe, and update-recipe endpoints.

### Authentication
Azure AD (CIAM) via `Microsoft.Identity.Web`. JWT bearer tokens. Auth is conditional — if `AzureAd:ClientId` is absent from config, authentication middleware is skipped (useful for local dev without Azure).

### Observability stack
Runs alongside the app in Docker Compose and Kubernetes:
- **OpenTelemetry Collector** — receives traces/metrics from the API via OTLP (port 4317), exports to Prometheus and Loki.
- **Prometheus** — scrapes the OTEL collector.
- **Loki** — receives structured logs.
- **Grafana** — dashboards provisioned from `grafana/provisioning/` and `grafana/dashboards/`.

Custom metrics are defined in `Cookbook.ApiService/Telemetry/CookbookMetrics.cs` using `System.Diagnostics.Metrics`. Meter name: `Cookbook.CustomMetrics`.

### Feature flags
Read from `appsettings.json` under `FeatureFlags`. Currently: `EnableBulkAddBoardRecipes`, `EnableUserProfiles`, `EnableProfilePictures`, `EnableRecipeImages`. Checked at startup and captured as local booleans that gate endpoint registration.

### CI/CD
- **`.github/workflows/ci.yml`** — build, lint, unit tests, integration tests, Docker image push to Docker Hub (`benhyer/cookbook-api`, `benhyer/cookbook-web`), Kubernetes deploy on `main`, Discord failure notification.
- **`.github/workflows/pg-backup.yml`** — daily `pg_dump` of the audit PostgreSQL database, stored at `/opt/cookbook-backups/` on the self-hosted runner, rotated to keep the 4 most recent files.
- **`.github/workflows/pr-deploy.yml`** — deploys PR preview environments to isolated Kubernetes namespaces.
- Deploy job runs on a `self-hosted` runner with `kubectl` access to the `cookbook` namespace.

### Kubernetes
Manifests in `k8s/`. Ingress routes two DuckDNS hosts to `cookbook-api`, `cookbook-web`, and `grafana`. Secrets (`k8s/secret.yaml`) are not committed — applied via CI using GitHub Actions secrets.

## Environment setup

Copy `.env.example` to `.env` before running Docker Compose. Required variables: `DB_PASSWORD`, `PG_PASSWORD`. Azure AD variables are optional for local development.
