using LabViroMol.IntegrationTests.Shared;
using LabViroMol.Modules.Research.Contracts;
using LabViroMol.Modules.Shared.Kernel.Identity;
using LabViroMol.Modules.Shared.Kernel.Primitives;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace LabViroMol.Modules.Inventory.IntegrationTests;

public class IntegrationTestWebAppFactory : LabViroMolWebAppFactory
{
    public IProjectChecker ProjectCheckerMock { get; } = Substitute.For<IProjectChecker>();

    public IntegrationTestWebAppFactory()
    {
        ProjectCheckerMock
            .IsEligibleForOrdersAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Success()));
        ProjectCheckerMock
            .IsEligibleForConsumptionAsync(Arg.Any<Guid>(), Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Success()));
    }

    protected override void ConfigureTestServices(IServiceCollection services)
    {
        var checkerDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IProjectChecker));
        if (checkerDescriptor != null) services.Remove(checkerDescriptor);

        services.AddSingleton(ProjectCheckerMock);
    }
}
