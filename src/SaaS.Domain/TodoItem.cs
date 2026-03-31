namespace SaaS.Domain;

/// <summary>Simple tenant-owned entity used to demonstrate and test tenant isolation.</summary>
public sealed class TodoItem : ITenantOwned
{
    public Guid Id { get; private set; }
    public TenantId TenantId { get; private set; } = null!;
    public string Title { get; private set; } = string.Empty;
    public bool IsDone { get; private set; }

    private TodoItem() { } // EF Core

    public TodoItem(TenantId tenantId, string title)
    {
        Id = Guid.NewGuid();
        TenantId = tenantId ?? throw new ArgumentNullException(nameof(tenantId));
        Title = !string.IsNullOrWhiteSpace(title)
            ? title
            : throw new ArgumentException("Title must not be empty.", nameof(title));
    }

    public void MarkDone() => IsDone = true;
}
