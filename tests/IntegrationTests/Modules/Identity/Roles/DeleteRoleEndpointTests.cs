using System.Net;
using System.Net.Http.Json;
using LabViroMol.Modules.Identity.Application.Users.ViewModels;
using LabViroMol.Modules.Identity.IntegrationTests.Users;
using LabViroMol.Modules.Identity.Presentation;
using LabViroMol.Modules.Shared.Kernel.Authorization;
using Microsoft.EntityFrameworkCore;

namespace LabViroMol.Modules.Identity.IntegrationTests.Roles;

public class DeleteRoleTests : BaseIdentityIntegrationTest
{
    public DeleteRoleTests(IdentityIntegrationTestWebAppFactory factory) : base(factory)
    {
        AuthenticateAs(Guid.NewGuid(), "admin@test.com", "Admin", "Test",
            [Permissions.Identity.RolesManage]);
    }

    [Fact]
    public async Task ShouldReturn204_AndRemoveRoleFromList_WhenRequestIsValid()
    {
        // Arrange
        var roleId = await UserDataSeeder.SeedRoleWithPermissionsAsync(
            RoleManager, "Pesquisador", [Permissions.Research.ProjectsView]);

        // Act
        var response = await Client.DeleteAsync($"/api/identity/roles/{roleId}");

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var roles = await Client.GetFromJsonAsync<List<RoleDetailViewModel>>("/api/identity/roles");
        Assert.DoesNotContain(roles!, r => r.Id == roleId);
    }

    [Fact]
    public async Task ShouldReturn404_WhenRoleDoesNotExist()
    {
        // Act
        var response = await Client.DeleteAsync($"/api/identity/roles/{Guid.NewGuid()}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ShouldReturn401_WhenNotAuthenticated()
    {
        // Arrange
        ClearAuthentication();

        // Act
        var response = await Client.DeleteAsync($"/api/identity/roles/{Guid.NewGuid()}");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ShouldRemoveRoleFromUsers_WhenUserHasRoleAssigned()
    {
        // Arrange
        var (userId, _) = await UserDataSeeder.SeedUserWithPasswordAsync(
            DbContext, UserManager, "researcher@test.com", "Password123!", "Joao", "Silva");

        var roleId = await UserDataSeeder.SeedRoleWithPermissionsAsync(
            RoleManager, "Pesquisador", [Permissions.Research.ProjectsView]);

        await UserDataSeeder.AssignRoleAsync(UserManager, userId, "Pesquisador");

        // Act
        var response = await Client.DeleteAsync($"/api/identity/roles/{roleId}");

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var remainingUserRoles = await DbContext.UserRoles
            .AsNoTracking()
            .Where(ur => ur.UserId == userId && ur.RoleId == roleId)
            .ToListAsync();
        Assert.Empty(remainingUserRoles);
    }
}
