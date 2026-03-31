# Copilot Agent Instructions (DDD + Clean Architecture)

## Goals
Build a multi-tenant SaaS starter using:
- .NET (latest LTS or current stable)
- PostgreSQL (Docker Compose for local dev)
- DDD + Clean Architecture boundaries
- High test coverage for tenant isolation, RBAC, quotas, and audit logging

## Non-negotiable architecture rules
1. **Domain is pure**: no EF Core, no ASP.NET, no logging frameworks. Only business logic.
2. **Application is orchestration**: use cases in Commands/Queries. Depends on Domain + interfaces (ports).
3. **Infrastructure implements ports**: EF Core, repositories, Postgres, external services.
4. **API and Worker are composition roots**: dependency injection wiring occurs here only.
5. Every tenant-owned entity must include `TenantId` and must never be queried cross-tenant.

## Required cross-cutting concerns
- Tenant resolution: `X-Tenant-Id` header (dev) and JWT claim (prod)
- EF Core global query filters for tenant entities
- SaveChanges guard to prevent wrong-tenant writes
- RBAC per tenant: Owner/Admin/Member/ReadOnly
- Rate limiting per tenant
- Daily quotas enforced for writes
- Audit events for all mutations
- OpenTelemetry (traces + metrics) minimal configuration

## Coding standards
- Prefer explicit types and clear naming over cleverness
- Prefer `Result`/exceptions consistently (pick one pattern and apply consistently)
- All public endpoints must have integration tests
- Avoid leaking EF Core entities outside Infrastructure
- Keep migrations in Infrastructure

## Testing requirements
- Unit tests for domain invariants and policies
- Integration tests using **Testcontainers + Postgres** for:
  - tenant isolation
  - RBAC enforcement
  - quota enforcement
  - audit logging persistence

## Deliverables and docs
- Root `README.md` with exact commands to run API, Worker, migrations, and tests
- `compose.yaml` for Postgres
- `BACKLOG.md` with acceptance criteria per item
