# Copilot Agent Instructions (Multi-tenant SaaS, .NET 10, DDD + Clean Architecture)

## Tech requirements
- Target **.NET 10**
- Language: C# (latest supported by .NET 10)
- Database: PostgreSQL (Docker Compose for local dev)
- EF Core + migrations in Infrastructure
- Integration tests: Testcontainers + Postgres

## Architecture (non-negotiable)
1. **Domain is pure** (no EF Core, ASP.NET, logging, configuration, DI container references).
2. **Application orchestrates**: Commands/Queries (use cases), DTOs, interfaces (ports), validators.
3. **Infrastructure implements ports**: EF Core, repositories, tenant provider impls, audit impls.
4. **API and Worker are composition roots**: DI wiring only here.
5. Tenant isolation is enforced in multiple layers: middleware + auth + EF query filters + SaveChanges guard.

## .NET / C# best practices (must follow)
- Enable nullable reference types and treat warnings as errors in CI.
- Use async/await correctly; pass CancellationToken through all async methods.
- Use Options pattern for configuration; no direct configuration reads scattered across code.
- Use ProblemDetails for error responses.
- Use HealthChecks for Postgres and app liveness.
- Do not expose EF Core entities outside Infrastructure; map to DTOs.
- Prefer DateTimeOffset in UTC for persisted timestamps.
- Keep controllers/endpoints thin; no business logic in API layer.

## Cross-cutting requirements
- Tenant resolution:
  - Dev: `X-Tenant-Id` header
  - Prod-style: TenantId from JWT claim
- EF Core global query filters for tenant-owned entities
- SaveChanges interceptor/guard to prevent wrong-tenant inserts/updates
- RBAC per tenant: Owner/Admin/Member/ReadOnly
- Rate limiting per tenant
- Daily quotas enforced for writes
- Audit logging for every mutation
- OpenTelemetry traces + metrics (tag `tenant.id`)

## Testing requirements
- Unit tests for domain invariants/policies
- Integration tests (Testcontainers Postgres) for:
  - tenant isolation
  - RBAC checks
  - quota enforcement
  - audit persistence

## Documentation requirements
- `README.md` must contain exact commands to:
  - start Postgres
  - run migrations
  - run API
  - run Worker
  - run tests
