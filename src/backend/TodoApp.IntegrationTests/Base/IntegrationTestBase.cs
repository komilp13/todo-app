using TodoApp.Api;
using Microsoft.AspNetCore.Mvc.Testing;

namespace TodoApp.IntegrationTests.Base;

/// <summary>
/// Base class for integration tests using WebApplicationFactory.
/// </summary>
public class IntegrationTestBase : IAsyncLifetime
{
    private WebApplicationFactory<Program>? _factory;
    protected HttpClient? Client { get; private set; }

    public async Task InitializeAsync()
    {
        _factory = new WebApplicationFactory<Program>();
        Client = _factory.CreateClient();
        await Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        if (Client != null)
        {
            Client.Dispose();
        }

        if (_factory != null)
        {
            await _factory.DisposeAsync();
        }
    }
}
