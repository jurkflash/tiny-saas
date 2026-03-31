# Multi-tenant SaaS Starter (.NET + Postgres) — DDD + Clean Architecture

## Prereqs
- .NET SDK
- Docker Desktop

## Start Postgres
```bash
docker compose up -d
```

## Run migrations
```bash
# (fill in exact dotnet ef commands)
```

## Run API
```bash
# (fill in exact command)
```

## Run Worker
```bash
# (fill in exact command)
```

## Run tests
```bash
dotnet test
```

## Dev auth / tenant selection
- Tenant resolution:
  - `X-Tenant-Id` header for local dev
  - JWT claim for production-style flows
