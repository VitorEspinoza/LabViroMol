using ArchUnitNET.Fluent;
using static ArchUnitNET.Fluent.ArchRuleDefinition;

namespace LabViroMol.ArchitectureTests;

public sealed class LayerDependencyTests
{
    [Fact]
    public void Domain_should_not_depend_on_other_layers()
    {
        Types().That().Are(ArchitectureModel.Domain)
            .Should().NotDependOnAny(ArchitectureModel.Application)
            .AndShould().NotDependOnAny(ArchitectureModel.Infrastructure)
            .AndShould().NotDependOnAny(ArchitectureModel.Presentation)
            .Check(ArchitectureModel.Architecture);
    }

    [Fact]
    public void Domain_should_not_depend_on_external_frameworks()
    {
        Types().That().Are(ArchitectureModel.Domain)
            .Should().NotDependOnAny(
                Types().That().ResideInAssembly("Microsoft.EntityFrameworkCore")
                    .Or().ResideInAssembly("Mediator.Abstractions")
                    .Or().ResideInAssembly("FluentValidation")
                    .Or().ResideInAssembly("Microsoft.AspNetCore.Http.Abstractions")
                    .Or().ResideInAssembly("Microsoft.AspNetCore.Routing"))
            .Check(ArchitectureModel.Architecture);
    }

    [Fact]
    public void Application_should_not_depend_on_infrastructure_or_presentation()
    {
        var businessInfrastructure = Types().That()
            .HaveFullNameContaining(".Infrastructure.")
            .And().DoNotHaveFullNameContaining("LabViroMol.Modules.Shared.Infrastructure.");

        Types().That().Are(ArchitectureModel.Application)
            .Should().NotDependOnAny(businessInfrastructure)
            .AndShould().NotDependOnAny(ArchitectureModel.Presentation)
            .Check(ArchitectureModel.Architecture);
    }

    [Fact]
    public void Application_should_not_depend_on_entity_framework_core()
    {
        Types().That().Are(ArchitectureModel.Application)
            .Should().NotDependOnAny(Types().That().ResideInAssembly("Microsoft.EntityFrameworkCore"))
            .Check(ArchitectureModel.Architecture);
    }

    [Fact]
    public void Infrastructure_should_not_depend_on_presentation()
    {
        Types().That().Are(ArchitectureModel.Infrastructure)
            .Should().NotDependOnAny(ArchitectureModel.Presentation)
            .Check(ArchitectureModel.Architecture);
    }

    [Fact]
    public void Presentation_should_not_depend_on_infrastructure_except_module_wiring()
    {
        Types().That()
            .Are(ArchitectureModel.Presentation)
            .And().DoNotHaveNameEndingWith("Module")
            .Should().NotDependOnAny(ArchitectureModel.Infrastructure)
            .Check(ArchitectureModel.Architecture);
    }
}
