# Multi-tenant SaaS Starter (.NET 10 + Postgres) — DDD + Clean Architecture

## Prereqs
- .NET 10 SDK
- Docker Desktop

## Start Postgres
```bash
docker compose up -d
```

## Run migrations
```bash
# Install the EF Core CLI tool (one-time)
dotnet tool install --global dotnet-ef

# Create a new migration (when model changes)
dotnet ef migrations add <MigrationName> \
  --project src/SaaS.Infrastructure \
  --startup-project src/SaaS.Api \
  --output-dir Persistence/Migrations

# Apply pending migrations to the database
dotnet ef database update \
  --project src/SaaS.Infrastructure \
  --startup-project src/SaaS.Api
```

## Run API
```bash
dotnet run --project src/SaaS.Api
```

## Run Worker
```bash
dotnet run --project src/SaaS.Worker
```

## Run tests
```bash
dotnet test
```

## Build
```bash
dotnet build
```

## Swagger UI
When running in Development mode, Swagger UI is available at:
```
https://localhost:{port}/swagger
```

## Health check
```
GET /healthz  →  200 OK (includes Npgsql connectivity check)
```

## Dev auth / tenant selection
- Tenant resolution:
  - `X-Tenant-Id` header for local dev
  - JWT claim for production-style flows

## CI

- **Unit tests** run automatically on every push to `main` and on pull requests.
- **Integration tests** run nightly at 02:00 UTC and can be [triggered manually](../../actions/workflows/ci-integration.yml) via `workflow_dispatch`.

## Architecture
```
SaaS.Domain          ← pure domain logic, no external dependencies
SaaS.Application     ← use cases, commands/queries, ports
SaaS.Infrastructure  ← EF Core, repositories, external service impls
SaaS.Api             ← ASP.NET Core Web API (composition root)
SaaS.Worker          ← Background Worker (composition root)
```
