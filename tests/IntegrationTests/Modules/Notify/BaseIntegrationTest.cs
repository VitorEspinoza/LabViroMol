using LabViroMol.IntegrationTests.Shared;
using LabViroMol.Modules.Notify.Infrastructure.Persistence;

namespace LabViroMol.Modules.Notify.IntegrationTests;

[CollectionDefinition("IntegrationTests")]
public class IntegrationTestCollection : ICollectionFixture<IntegrationTestWebAppFactory> { }

[Collection("IntegrationTests")]
public abstract class BaseIntegrationTest : BaseIntegrationTest<NotifyDbContext>
{
    public const string TargetPermission = "Notify.Test.Permission";

    protected BaseIntegrationTest(IntegrationTestWebAppFactory factory) : base(factory)
    {
        AuthenticateAsUser();
    }

    protected Guid AuthenticateAsUser(Guid? userId = null)
    {
        var id = userId ?? Guid.NewGuid();
        AuthenticateAs([TargetPermission], id);
        return id;
    }
}
