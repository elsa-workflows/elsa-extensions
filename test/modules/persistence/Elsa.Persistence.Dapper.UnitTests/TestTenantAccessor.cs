using Elsa.Common.Multitenancy;

namespace Elsa.Persistence.Dapper.UnitTests;

internal sealed class TestTenantAccessor : ITenantAccessor
{
    public string TenantId => Tenant?.Id ?? Tenant.DefaultTenantId;
    public Tenant? Tenant { get; private set; }

    public IDisposable PushContext(Tenant? tenant)
    {
        var previousTenant = Tenant;
        Tenant = tenant;
        return new Restore(() => Tenant = previousTenant);
    }

    private sealed class Restore(Action restore) : IDisposable
    {
        public void Dispose() => restore();
    }
}
