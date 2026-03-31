namespace SaaS.Domain;

/// <summary>Strongly-typed identifier for a tenant.</summary>
public sealed record TenantId(Guid Value)
{
    public static TenantId New() => new(Guid.NewGuid());

    public static TenantId Parse(string value) =>
        Guid.TryParse(value, out var id)
            ? new TenantId(id)
            : throw new ArgumentException($"'{value}' is not a valid TenantId.", nameof(value));

    public override string ToString() => Value.ToString();
}
