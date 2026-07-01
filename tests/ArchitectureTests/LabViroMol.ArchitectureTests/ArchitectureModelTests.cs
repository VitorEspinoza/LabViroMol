using ArchUnitNET.Domain;

namespace LabViroMol.ArchitectureTests;

public sealed class ArchitectureModelTests
{
    [Fact]
    public void Architecture_model_loads_all_assemblies()
    {
        Assert.NotNull(ArchitectureModel.Architecture);
        Assert.NotEmpty(ArchitectureModel.Architecture.Types);
    }
}
