# Backlog (run top-to-bottom)

## 0. Solution skeleton
**Goal:** Create solution + projects + references + build/test green.
**Acceptance criteria**
- `dotnet build` succeeds
- `dotnet test` succeeds (even if minimal tests)
- Clean Architecture dependencies enforced (Domain not referencing Infrastructure)

## 1. Local Postgres (Docker Compose)
**Goal:** Add `compose.yaml` and connection string conventions.
**Acceptance criteria**
- `docker compose up -d` starts Postgres
- API can connect using env vars
- README includes exact commands

## 2. EF Core baseline + migrations
**Goal:** Add DbContext in Infrastructure and initial migration.
**Acceptance criteria**
- `dotnet ef migrations add Initial` works (documented command)
- `dotnet ef database update` works
- Migration files live in `src/SaaS.Infrastructure`

## 3. Tenant context + resolution
**Goal:** Resolve `TenantId` from `X-Tenant-Id` header (dev) and JWT claim (prod).
**Acceptance criteria**
- Middleware populates `ITenantContext`
- Requests without tenant are rejected with clear error
- Tests cover missing/invalid tenant

## 4. Tenant isolation enforcement (hard requirement)
**Goal:** Ensure tenant-owned entities can never leak.
**Acceptance criteria**
- EF Core global query filters for all tenant entities
- SaveChanges interceptor validates TenantId on inserts/updates
- Integration tests: create in Tenant A cannot read in Tenant B

## 5. Identity + membership model
**Goal:** Add Users, Tenants, TenantUsers with roles.
**Acceptance criteria**
- Seed a dev tenant + owner user for local use
- Role checks in Application layer
- Integration tests verify Member vs Admin permissions

## 6. Notes + Tasks features (minimal product)
**Goal:** CRUD Notes and Tasks with RBAC.
**Acceptance criteria**
- Member can create/update
- ReadOnly cannot mutate
- Audit events written for each mutation

## 7. Audit logging
**Goal:** Persist audit events for mutations.
**Acceptance criteria**
- Audit writer port in Application, impl in Infrastructure
- Admin endpoint to query audit events
- Tests verify audit entries exist and include tenant + actor + entity id

## 8. Rate limiting (per tenant)
**Goal:** Limit requests/minute based on tenant plan.
**Acceptance criteria**
- Partition key = TenantId
- Different limits for Free vs Pro
- Tests (or minimal verification harness) added

## 9. Usage metering + daily quotas
**Goal:** Record usage events and enforce daily write quota.
**Acceptance criteria**
- Counter increments on write operations
- Worker aggregates daily usage into `UsageDaily`
- When over quota, writes return 429/403 with actionable message
- Integration tests cover quota exceeded

## 10. Observability
**Goal:** OpenTelemetry traces/metrics wired for API + Worker.
**Acceptance criteria**
- OTEL enabled with simple exporter (console/OTLP)
- Trace includes TenantId tag
- README documents how to view basic outputs
