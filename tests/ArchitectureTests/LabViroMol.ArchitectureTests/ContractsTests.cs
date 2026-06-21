using static ArchUnitNET.Fluent.ArchRuleDefinition;

namespace LabViroMol.ArchitectureTests;

public sealed class ContractsTests
{
    [Fact]
    public void Contracts_should_not_depend_on_domain_application_infrastructure_or_presentation_of_the_same_module()
    {
        foreach (var module in TestTypeCatalog.ModulesWithContracts())
        {
            Types().That().HaveFullNameContaining($"LabViroMol.Modules.{module}.Contracts")
                .Should().NotDependOnAny(
                    Types().That().HaveFullNameContaining($"LabViroMol.Modules.{module}.Domain.")
                        .Or().HaveFullNameContaining($"LabViroMol.Modules.{module}.Application.")
                        .Or().HaveFullNameContaining($"LabViroMol.Modules.{module}.Infrastructure.")
                        .Or().HaveFullNameContaining($"LabViroMol.Modules.{module}.Presentation."))
                .Check(ArchitectureModel.Architecture);
        }
    }

    [Fact]
    public void Concrete_contract_types_should_be_records()
    {
        var contracts = TestTypeCatalog.ContractsTypes()
            .Where(type => type is { IsClass: true, IsAbstract: false })
            .ToArray();

        Assert.All(contracts, type => Assert.True(SourceCodeInspector.IsDeclaredAsRecord(type), $"{type.FullName} should be declared as record."));
    }
}
