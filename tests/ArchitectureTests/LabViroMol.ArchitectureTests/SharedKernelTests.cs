using static ArchUnitNET.Fluent.ArchRuleDefinition;

namespace LabViroMol.ArchitectureTests;

public sealed class SharedKernelTests
{
    [Fact]
    public void Shared_kernel_should_not_depend_on_business_modules()
    {
        foreach (var module in ArchitectureModel.BusinessModules)
        {
            Types().That().Are(ArchitectureModel.SharedKernel)
                .Should().NotDependOnAny(
                    Types().That().HaveFullNameContaining($"LabViroMol.Modules.{module}.")
                        .And().DoNotHaveFullNameContaining("LabViroMol.Modules.Shared."))
                .Check(ArchitectureModel.Architecture);
        }
    }

    [Fact]
    public void Shared_kernel_should_not_depend_on_framework_web_or_ef_types()
    {
        Types().That().Are(ArchitectureModel.SharedKernel)
            .Should().NotDependOnAny(
                Types().That().ResideInAssembly("Microsoft.EntityFrameworkCore")
                    .Or().ResideInAssembly("Microsoft.AspNetCore.Http.Abstractions")
                    .Or().ResideInAssembly("Microsoft.AspNetCore.Routing"))
            .Check(ArchitectureModel.Architecture);
    }

    [Fact]
    public void Shared_infrastructure_should_not_depend_on_business_modules()
    {
        foreach (var module in ArchitectureModel.BusinessModules)
        {
            Types().That().Are(ArchitectureModel.SharedInfrastructure)
                .Should().NotDependOnAny(
                    Types().That().HaveFullNameContaining($"LabViroMol.Modules.{module}.")
                        .And().DoNotHaveFullNameContaining("LabViroMol.Modules.Shared."))
                .Check(ArchitectureModel.Architecture);
        }
    }
}
