using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SaaS.Infrastructure.Persistence;
using Testcontainers.PostgreSql;

namespace SaaS.Infrastructure.IntegrationTests;

public sealed class TenantIsolationTests : IAsyncLifetime
{
    private static readonly string TenantA = Guid.NewGuid().ToString();
    private static readonly string TenantB = Guid.NewGuid().ToString();

    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:17")
        .Build();

    private WebApplicationFactory<Program> _factory = null!;

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Development");
                builder.UseSetting(
                    "ConnectionStrings:DefaultConnection",
                    _postgres.GetConnectionString());
            });

        // Apply migrations.
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        await _factory.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    // ── Tenant resolution ──────────────────────────────────────────

    [Fact]
    public async Task Request_without_tenant_header_returns_400_ProblemDetails()
    {
        using var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/todoitems");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal(400, problem.Status);
        Assert.Contains("tenant", problem.Title, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Request_with_invalid_tenant_header_returns_400()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Tenant-Id", "not-a-guid");

        var response = await client.GetAsync("/api/todoitems");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ── Cross-tenant read isolation ────────────────────────────────

    [Fact]
    public async Task Tenant_A_cannot_read_Tenant_B_items()
    {
        using var clientA = CreateClientForTenant(TenantA);
        using var clientB = CreateClientForTenant(TenantB);

        // Tenant A creates an item.
        var createResponse = await clientA.PostAsJsonAsync(
            "/api/todoitems", new { Title = "TenantA item" });
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        // Tenant B sees zero items.
        var itemsB = await clientB.GetFromJsonAsync<List<TodoItemDto>>("/api/todoitems");
        Assert.NotNull(itemsB);
        Assert.Empty(itemsB);

        // Tenant A sees exactly one item.
        var itemsA = await clientA.GetFromJsonAsync<List<TodoItemDto>>("/api/todoitems");
        Assert.NotNull(itemsA);
        Assert.Single(itemsA);
    }

    [Fact]
    public async Task Tenant_B_cannot_read_Tenant_A_item_by_id()
    {
        using var clientA = CreateClientForTenant(TenantA);
        using var clientB = CreateClientForTenant(TenantB);

        // Tenant A creates an item.
        var createResponse = await clientA.PostAsJsonAsync(
            "/api/todoitems", new { Title = "Only A should see this" });
        var created = await createResponse.Content.ReadFromJsonAsync<TodoItemDto>();
        Assert.NotNull(created);

        // Tenant B tries to fetch by ID → 404 because of global query filter.
        var response = await clientB.GetAsync($"/api/todoitems/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ── Cross-tenant write isolation ───────────────────────────────

    [Fact]
    public async Task SaveChanges_guard_prevents_cross_tenant_write()
    {
        // Arrange: seed an item for Tenant A directly in the DB, then
        // attempt to modify it under Tenant B's context.
        var tenantAId = Guid.Parse(TenantA);
        var tenantBId = Guid.Parse(TenantB);

        var itemId = Guid.NewGuid();

        // Insert directly to bypass the query filter.
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await db.Database.ExecuteSqlInterpolatedAsync(
                $"INSERT INTO \"TodoItems\" (\"Id\", \"TenantId\", \"Title\", \"IsDone\") VALUES ({itemId}, {tenantAId}, 'raw-insert', false)");
        }

        // Under Tenant B's context, attempt to track & save the entity.
        using (var scope = _factory.Services.CreateScope())
        {
            var tenantContext = scope.ServiceProvider.GetRequiredService<TenantContext>();
            tenantContext.SetTenantId(new SaaS.Domain.TenantId(tenantBId));

            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            // Use IgnoreQueryFilters to load the entity that belongs to Tenant A.
            var item = await db.TodoItems
                .IgnoreQueryFilters()
                .FirstAsync(t => t.Id == itemId);

            item.MarkDone(); // modify it

            // SaveChanges should throw because the entity belongs to Tenant A
            // but the current tenant is B.
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                () => db.SaveChangesAsync());
            Assert.Contains("Cross-tenant writes are not allowed", ex.Message);
        }
    }

    // ── Each tenant sees only their own data ───────────────────────

    [Fact]
    public async Task Each_tenant_only_sees_own_items()
    {
        using var clientA = CreateClientForTenant(TenantA);
        using var clientB = CreateClientForTenant(TenantB);

        // Both tenants create items.
        await clientA.PostAsJsonAsync("/api/todoitems", new { Title = "A-1" });
        await clientA.PostAsJsonAsync("/api/todoitems", new { Title = "A-2" });
        await clientB.PostAsJsonAsync("/api/todoitems", new { Title = "B-1" });

        var aItems = await clientA.GetFromJsonAsync<List<TodoItemDto>>("/api/todoitems");
        var bItems = await clientB.GetFromJsonAsync<List<TodoItemDto>>("/api/todoitems");

        Assert.NotNull(aItems);
        Assert.NotNull(bItems);

        // A sees all items created in A context across this and previous tests
        Assert.All(aItems, i => Assert.DoesNotContain("B-", i.Title));
        // B sees only B items
        Assert.All(bItems, i => Assert.StartsWith("B-", i.Title));
    }

    // ── Health check still works (no tenant required) ──────────────

    [Fact]
    public async Task Healthz_still_works_without_tenant_header()
    {
        using var client = _factory.CreateClient();
        var response = await client.GetAsync("/healthz");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // ── Helpers ────────────────────────────────────────────────────

    private HttpClient CreateClientForTenant(string tenantId)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Tenant-Id", tenantId);
        return client;
    }

    private sealed record TodoItemDto(Guid Id, string Title, bool IsDone);
}
