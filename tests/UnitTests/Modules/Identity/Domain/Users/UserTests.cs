using LabViroMol.Modules.Identity.Domain.UnitTests.Common;
using LabViroMol.Modules.Identity.Domain.Users;
using LabViroMol.Modules.Shared.Kernel.Primitives;

namespace LabViroMol.Modules.Identity.Domain.UnitTests.Users;

public class UserTests
{
    public class CreateTests
    {
        [Fact]
        public void Create_WithValidInputs_ShouldSetPropertiesCorrectly()
        {
            // Arrange
            var id = Fakers.AnyUserId();
            var name = Fakers.AnyUserName();
            var email = Fakers.AnyEmail();

            // Act
            var user = User.Create(id, name, email, null, null);

            // Assert
            Assert.Equal(id, user.Id);
            Assert.Equal(name, user.Name);
            Assert.Equal(email, user.Email);
            Assert.True(user.IsActive);
            Assert.Null(user.DeactivatedAt);
        }

        [Fact]
        public void Create_WithPhoneAndEmergency_ShouldTrimWhitespace()
        {
            // Arrange
            var id = Fakers.AnyUserId();
            var name = Fakers.AnyUserName();
            var email = Fakers.AnyEmail();

            // Act
            var user = User.Create(id, name, email, "  123456  ", "  789012  ");

            // Assert
            Assert.Equal("123456", user.PhoneNumber);
            Assert.Equal("789012", user.EmergencyContactNumber);
        }
    }

    public class UpdateTests
    {
        [Fact]
        public void Update_WithValidInputs_ShouldChangeProperties()
        {
            // Arrange
            var user = Fakers.CreateUser();
            var newName = Fakers.AnyUserName();
            var newEmail = Fakers.AnyEmail();

            // Act
            user.Update(newName, newEmail, "111111", "222222");

            // Assert
            Assert.Equal(newName, user.Name);
            Assert.Equal(newEmail, user.Email);
            Assert.Equal("111111", user.PhoneNumber);
            Assert.Equal("222222", user.EmergencyContactNumber);
        }

        [Fact]
        public void Update_WithPhoneAndEmergency_ShouldTrimWhitespace()
        {
            // Arrange
            var user = Fakers.CreateUser();

            // Act
            user.Update(Fakers.AnyUserName(), Fakers.AnyEmail(), "  111  ", "  222  ");

            // Assert
            Assert.Equal("111", user.PhoneNumber);
            Assert.Equal("222", user.EmergencyContactNumber);
        }
    }

    public class DeactivateTests
    {
        [Fact]
        public void Deactivate_WhenActive_ShouldSetDeactivatedAtAndIsActiveFalse()
        {
            // Arrange
            var user = Fakers.CreateUser();

            // Act
            user.Deactivate();

            // Assert
            Assert.NotNull(user.DeactivatedAt);
            Assert.False(user.IsActive);
        }

        [Fact]
        public void Deactivate_WhenAlreadyDeactivated_ShouldBeIdempotent()
        {
            // Arrange
            var user = Fakers.CreateDeactivatedUser();
            var originalDeactivatedAt = user.DeactivatedAt;

            // Act
            user.Deactivate();

            // Assert
            Assert.False(user.IsActive);
            Assert.Equal(originalDeactivatedAt, user.DeactivatedAt);
        }
    }

    public class ReactivateTests
    {
        [Fact]
        public void Reactivate_WhenDeactivated_ShouldClearDeactivatedAtAndIsActiveTrue()
        {
            // Arrange
            var user = Fakers.CreateDeactivatedUser();

            // Act
            user.Reactivate();

            // Assert
            Assert.Null(user.DeactivatedAt);
            Assert.True(user.IsActive);
        }

        [Fact]
        public void Reactivate_WhenAlreadyActive_ShouldThrowDomainException()
        {
            // Arrange
            var user = Fakers.CreateUser();

            // Act & Assert
            var ex = Assert.Throws<DomainException>(() => user.Reactivate());
            Assert.Equal("O usuário já está ativo.", ex.Message);
        }
    }
}
