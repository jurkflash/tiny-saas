namespace SaaS.Domain;

/// <summary>
/// Marker interface for entities that belong to a specific tenant.
/// EF Core global query filters and the SaveChanges guard use this
/// to enforce tenant isolation automatically.
/// </summary>
public interface ITenantOwned
{
    TenantId TenantId { get; }
}
