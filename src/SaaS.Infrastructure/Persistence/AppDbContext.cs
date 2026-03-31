using System.Reflection;
using Microsoft.EntityFrameworkCore;
using SaaS.Application;
using SaaS.Domain;

namespace SaaS.Infrastructure.Persistence;

public sealed class AppDbContext : DbContext
{
    private readonly ITenantContext _tenantContext;

    /// <summary>
    /// Backing property read by the EF Core query filter expression.
    /// EF Core parameterizes it on every query because the filter lambda
    /// captures <c>this</c> (the DbContext) via a closure.
    /// Uses <see cref="TenantId"/> (not raw Guid) so the value converter is applied correctly.
    /// </summary>
    private TenantId CurrentTenantId => _tenantContext.TenantId;

    public AppDbContext(DbContextOptions<AppDbContext> options, ITenantContext tenantContext)
        : base(options)
    {
        _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
    }

    public DbSet<TodoItem> TodoItems => Set<TodoItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        // Apply global query filters to every entity that implements ITenantOwned.
        var configureMethod = typeof(AppDbContext)
            .GetMethod(nameof(ConfigureTenantFilter), BindingFlags.Instance | BindingFlags.NonPublic)!;

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (!typeof(ITenantOwned).IsAssignableFrom(entityType.ClrType))
                continue;

            configureMethod
                .MakeGenericMethod(entityType.ClrType)
                .Invoke(this, [modelBuilder]);
        }

        // Configure TenantId value conversion on TodoItem.
        modelBuilder.Entity<TodoItem>(b =>
        {
            b.HasKey(t => t.Id);
            b.Property(t => t.TenantId)
             .HasConversion(v => v.Value, v => new TenantId(v))
             .IsRequired();
            b.HasIndex(t => t.TenantId);
            b.Property(t => t.Title).HasMaxLength(500).IsRequired();
        });
    }

    /// <summary>
    /// Adds a global query filter for the given tenant-owned entity type.
    /// The lambda captures <c>this</c> so EF Core re-evaluates
    /// <see cref="CurrentTenantGuid"/> for every query.
    /// </summary>
    private void ConfigureTenantFilter<T>(ModelBuilder modelBuilder) where T : class, ITenantOwned
    {
        modelBuilder.Entity<T>().HasQueryFilter(e => e.TenantId == CurrentTenantId);
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        EnforceTenantOwnership();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        EnforceTenantOwnership();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    /// <summary>
    /// Ensures every added or modified tenant-owned entity has the correct TenantId.
    /// Blocks cross-tenant writes at the persistence boundary.
    /// </summary>
    private void EnforceTenantOwnership()
    {
        var currentTenantId = _tenantContext.TenantId;

        foreach (var entry in ChangeTracker.Entries<ITenantOwned>())
        {
            if (entry.State is EntityState.Added or EntityState.Modified)
            {
                if (entry.Entity.TenantId != currentTenantId)
                {
                    throw new InvalidOperationException(
                        $"Entity {entry.Entity.GetType().Name} has TenantId " +
                        $"'{entry.Entity.TenantId}' but the current tenant is '{currentTenantId}'. " +
                        "Cross-tenant writes are not allowed.");
                }
            }
        }
    }
}
