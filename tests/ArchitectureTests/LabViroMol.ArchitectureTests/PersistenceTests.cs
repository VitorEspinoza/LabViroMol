using Microsoft.EntityFrameworkCore;
using static ArchUnitNET.Fluent.ArchRuleDefinition;

namespace LabViroMol.ArchitectureTests;

public sealed class PersistenceTests
{
    [Fact]
    public void Dbcontexts_should_only_exist_in_infrastructure_and_use_dbcontext_suffix()
    {
        var dbContexts = TestTypeCatalog.AllTypes()
            .Where(type => type.IsClass && !type.IsAbstract && typeof(DbContext).IsAssignableFrom(type))
            .ToArray();

        Assert.All(dbContexts, type =>
        {
            Assert.Contains(".Infrastructure.", type.FullName);
            Assert.EndsWith("DbContext", type.Name, StringComparison.Ordinal);
        });
    }

    [Fact]
    public void Non_infrastructure_layers_should_not_depend_on_entity_framework_core()
    {
        Types().That().Are(ArchitectureModel.Domain)
            .Or().Are(ArchitectureModel.Application)
            .Or().Are(ArchitectureModel.Presentation)
            .Should().NotDependOnAny(Types().That().ResideInAssembly("Microsoft.EntityFrameworkCore"))
            .Check(ArchitectureModel.Architecture);
    }

    [Fact]
    public void Entity_type_configurations_should_live_in_infrastructure()
    {
        Assert.All(TestTypeCatalog.EntityFrameworkConfigurations(), type => Assert.Contains(".Infrastructure.", type.FullName));
    }
}
