using System.Net.Http.Headers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LabViroMol.IntegrationTests.Shared;

public abstract class BaseIntegrationTest<TDbContext> : IAsyncLifetime
    where TDbContext : DbContext
{
    protected readonly HttpClient Client;
    protected readonly LabViroMolWebAppFactory Factory;
    private readonly IServiceScope _scope;
    protected readonly TDbContext DbContext;

    protected BaseIntegrationTest(LabViroMolWebAppFactory factory)
    {
        Factory = factory;
        Client = factory.CreateClient();
        _scope = factory.Services.CreateScope();
        DbContext = _scope.ServiceProvider.GetRequiredService<TDbContext>();
    }

    protected void AuthenticateAs(
        IEnumerable<string> permissions,
        Guid? userId = null,
        string email = "admin@test.com",
        string firstName = "Admin",
        string lastName = "Test") =>
        Client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", TestJwt.Generate(permissions, userId, email, firstName, lastName));

    protected void ClearAuthentication() =>
        Client.DefaultRequestHeaders.Authorization = null;

    public virtual Task InitializeAsync() => Task.CompletedTask;

    public virtual async Task DisposeAsync()
    {
        _scope.Dispose();
        await Factory.ResetDatabaseAsync();
    }
}
