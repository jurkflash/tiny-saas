namespace SaaS.Domain.UnitTests;

public sealed class TenantIdTests
{
    [Fact]
    public void New_returns_non_empty_id()
    {
        var id = TenantId.New();
        Assert.NotEqual(Guid.Empty, id.Value);
    }

    [Fact]
    public void Parse_valid_guid_returns_tenant_id()
    {
        var guid = Guid.NewGuid();
        var id = TenantId.Parse(guid.ToString());
        Assert.Equal(guid, id.Value);
    }

    [Fact]
    public void Parse_invalid_string_throws_argument_exception()
    {
        Assert.Throws<ArgumentException>(() => TenantId.Parse("not-a-guid"));
    }

    [Fact]
    public void ToString_returns_guid_string()
    {
        var guid = Guid.NewGuid();
        var id = new TenantId(guid);
        Assert.Equal(guid.ToString(), id.ToString());
    }

    [Fact]
    public void Equality_is_value_based()
    {
        var guid = Guid.NewGuid();
        Assert.Equal(new TenantId(guid), new TenantId(guid));
        Assert.NotEqual(TenantId.New(), TenantId.New());
    }
}
