using LabViroMol.Modules.Identity.Domain.Users;
using LabViroMol.Modules.Shared.Kernel.Primitives;
using Xunit;

namespace LabViroMol.Modules.Identity.Domain.UnitTests.Users;

public class EmailTests
{
    [Fact]
    public void Constructor_WithValidEmail_ShouldStoreTrimmedLowercase()
    {
        // Act
        var email = new Email("  Test@Example.COM  ");

        // Assert
        Assert.Equal("test@example.com", email.Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Constructor_WithEmptyOrNull_ShouldThrowDomainException(string? value)
    {
        // Act & Assert
        Assert.Throws<DomainException>(() => new Email(value!));
    }

    [Fact]
    public void Constructor_WithoutAtSymbol_ShouldThrowDomainException()
    {
        // Act & Assert
        var ex = Assert.Throws<DomainException>(() => new Email("invalidemail.com"));
        Assert.Equal("Formato de e-mail inválido.", ex.Message);
    }
}
