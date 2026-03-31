using SaaS.Domain;

namespace SaaS.Application;

/// <summary>
/// Provides access to the current tenant for the scope of a request.
/// Implementations resolve the tenant from the HTTP request (header / JWT claim).
/// </summary>
public interface ITenantContext
{
    TenantId TenantId { get; }
}
