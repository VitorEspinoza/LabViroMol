using LabViroMol.Modules.Identity.Domain.Users;
using LabViroMol.Modules.Shared.Kernel.Primitives;

namespace LabViroMol.Modules.Identity.Domain.UnitTests.Users;

public class EmergencyContactTests
{
    [Fact]
    public void Constructor_ShouldTrimWhitespace()
    {
        // Act
        var contact = new EmergencyContact("  Maria  ", "  11999999999  ");

        // Assert
        Assert.Equal("Maria", contact.Name);
        Assert.Equal("11999999999", contact.Number);
    }

    [Theory]
    [InlineData("", "11999999999")]
    [InlineData(null, "11999999999")]
    public void Constructor_WithEmptyName_ShouldThrowDomainException(string? name, string number)
    {
        // Act & Assert
        Assert.Throws<DomainException>(() => new EmergencyContact(name!, number));
    }

    [Theory]
    [InlineData("Maria", "")]
    [InlineData("Maria", null)]
    public void Constructor_WithEmptyNumber_ShouldThrowDomainException(string name, string? number)
    {
        // Act & Assert
        Assert.Throws<DomainException>(() => new EmergencyContact(name, number!));
    }

    [Fact]
    public void FromNullable_WithBothEmpty_ShouldReturnNull()
    {
        // Act
        var contact = EmergencyContact.FromNullable(null, null);

        // Assert
        Assert.Null(contact);
    }

    [Fact]
    public void FromNullable_WithBothProvided_ShouldReturnInstance()
    {
        // Act
        var contact = EmergencyContact.FromNullable("Maria", "11999999999");

        // Assert
        Assert.NotNull(contact);
        Assert.Equal("Maria", contact!.Name);
        Assert.Equal("11999999999", contact.Number);
    }
}
