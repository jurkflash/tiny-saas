using SaaS.Application;
using SaaS.Domain;

namespace SaaS.Infrastructure.Persistence;

/// <summary>
/// Scoped implementation of <see cref="ITenantContext"/>.
/// The tenant resolution middleware sets <see cref="TenantId"/> once per request.
/// </summary>
public sealed class TenantContext : ITenantContext
{
    private TenantId? _tenantId;

    public TenantId TenantId =>
        _tenantId ?? throw new InvalidOperationException("TenantId has not been set for the current scope.");

    public void SetTenantId(TenantId tenantId) =>
        _tenantId = tenantId ?? throw new ArgumentNullException(nameof(tenantId));
}
