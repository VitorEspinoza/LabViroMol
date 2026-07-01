using static ArchUnitNET.Fluent.ArchRuleDefinition;

namespace LabViroMol.ArchitectureTests;

public sealed class PresentationTests
{
    [Fact]
    public void Presentation_should_not_depend_on_entity_framework_core()
    {
        Types().That().Are(ArchitectureModel.Presentation)
            .Should().NotDependOnAny(Types().That().ResideInAssembly("Microsoft.EntityFrameworkCore"))
            .Check(ArchitectureModel.Architecture);
    }

    [Fact]
    public void Presentation_should_not_depend_on_concrete_repositories()
    {
        var concreteRepositories = Classes().That()
            .HaveNameEndingWith("Repository")
            .And().HaveFullNameContaining(".Infrastructure.");

        Types().That().Are(ArchitectureModel.Presentation)
            .Should().NotDependOnAny(concreteRepositories)
            .Check(ArchitectureModel.Architecture);
    }
}
