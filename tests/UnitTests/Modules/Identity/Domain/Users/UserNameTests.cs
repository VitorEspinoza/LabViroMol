using LabViroMol.Modules.Identity.Domain.Users;
using LabViroMol.Modules.Shared.Kernel.Primitives;
using Xunit;

namespace LabViroMol.Modules.Identity.Domain.UnitTests.Users;

public class UserNameTests
{
    [Fact]
    public void Constructor_WithValidNames_ShouldStoreTrimmedAndFullName()
    {
        // Act
        var name = new UserName("  João  ", "  Silva  ");

        // Assert
        Assert.Equal("João", name.FirstName);
        Assert.Equal("Silva", name.LastName);
        Assert.Equal("João Silva", name.FullName);
    }

    [Theory]
    [InlineData("", "Last")]
    [InlineData("First", "")]
    [InlineData(null, "Last")]
    [InlineData("First", null)]
    public void Constructor_WithEmptyOrNullNames_ShouldThrowDomainException(string? firstName, string? lastName)
    {
        // Act & Assert
        Assert.Throws<DomainException>(() => new UserName(firstName!, lastName!));
    }
}
