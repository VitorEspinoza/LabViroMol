using LabViroMol.Modules.Inventory.Domain.Materials;
using LabViroMol.Modules.Shared.Kernel.Primitives;

namespace LabViroMol.Modules.Inventory.Domain.UnitTests.Materials;

public class QuantityTests
{
    [Fact]
    public void Constructor_WhenValueIsNonNegative_ShouldSetValue()
    {
        // Assert
        Assert.Equal(42m,    new Quantity(42m).Value);
        Assert.Equal(0m,     new Quantity(0m).Value);
        Assert.Equal(0.001m, new Quantity(0.001m).Value);
    }

    [Fact]
    public void Constructor_WhenValueIsNegative_ShouldThrowDomainException()
    {
        Assert.Throws<DomainException>(() => new Quantity(-5m));
    }
}
