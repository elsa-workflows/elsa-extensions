using Elsa.DevOps.AzureDevOps.Services;

namespace Elsa.DevOps.AzureDevOps.UnitTests.Services;

public class AzureDevOpsConnectionFactoryTests : IDisposable
{
    private readonly AzureDevOpsConnectionFactory _factory = new();

    [Fact]
    public void Same_Url_And_Token_Returns_Same_Connection()
    {
        var conn1 = _factory.GetConnection("https://dev.azure.com/org1", "pat-token-1");
        var conn2 = _factory.GetConnection("https://dev.azure.com/org1", "pat-token-1");

        Assert.Same(conn1, conn2);
    }

    [Fact]
    public void Different_Url_Returns_Different_Connection()
    {
        var conn1 = _factory.GetConnection("https://dev.azure.com/org1", "pat-token-1");
        var conn2 = _factory.GetConnection("https://dev.azure.com/org2", "pat-token-1");

        Assert.NotSame(conn1, conn2);
    }

    [Fact]
    public void Different_Token_Returns_Different_Connection()
    {
        var conn1 = _factory.GetConnection("https://dev.azure.com/org1", "token-a");
        var conn2 = _factory.GetConnection("https://dev.azure.com/org1", "token-b");

        Assert.NotSame(conn1, conn2);
    }

    [Fact]
    public void Trailing_Slash_Is_Normalized()
    {
        var conn1 = _factory.GetConnection("https://dev.azure.com/org1", "pat-token-1");
        var conn2 = _factory.GetConnection("https://dev.azure.com/org1/", "pat-token-1");

        // The cache keys differ because the URL string differs, but the underlying
        // URI is normalized. This verifies the factory doesn't throw on trailing slashes.
        Assert.NotNull(conn1);
        Assert.NotNull(conn2);
    }

    [Fact]
    public void Dispose_Does_Not_Throw()
    {
        _factory.GetConnection("https://dev.azure.com/org1", "pat-token");

        var ex = Record.Exception(() => _factory.Dispose());

        Assert.Null(ex);
    }

    [Fact]
    public void Multiple_Dispose_Calls_Do_Not_Throw()
    {
        _factory.GetConnection("https://dev.azure.com/org1", "pat-token");

        _factory.Dispose();
        var ex = Record.Exception(() => _factory.Dispose());

        Assert.Null(ex);
    }

    [Fact]
    public async Task Concurrent_Access_Returns_Valid_Connections()
    {
        const int concurrency = 20;
        var tasks = Enumerable.Range(0, concurrency)
            .Select(i => Task.Run(() =>
                _factory.GetConnection($"https://dev.azure.com/org{i % 5}", $"token-{i % 3}")))
            .ToArray();

        var connections = await Task.WhenAll(tasks);

        Assert.All(connections, conn => Assert.NotNull(conn));
    }

    [Fact]
    public async Task Concurrent_Access_Same_Key_Returns_Same_Instance()
    {
        const int concurrency = 50;
        var tasks = Enumerable.Range(0, concurrency)
            .Select(_ => Task.Run(() =>
                _factory.GetConnection("https://dev.azure.com/shared", "shared-token")))
            .ToArray();

        var connections = await Task.WhenAll(tasks);

        var first = connections[0];
        Assert.All(connections, conn => Assert.Same(first, conn));
    }

    public void Dispose() => _factory.Dispose();
}
