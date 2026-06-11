using System.Net;
using System.Net.Http.Json;
using LabViroMol.Modules.Identity.Application.Users.ViewModels;
using LabViroMol.Modules.Identity.Presentation;
using LabViroMol.Modules.Identity.IntegrationTests.Users;
using LabViroMol.Modules.Shared.Kernel.Authorization;

namespace LabViroMol.Modules.Identity.IntegrationTests.Roles;

public class UpdateRolePermissionsTests : BaseIdentityIntegrationTest
{
    public UpdateRolePermissionsTests(IdentityIntegrationTestWebAppFactory factory) : base(factory)
    {
        AuthenticateAs(Guid.NewGuid(), "admin@test.com", "Admin", "Test",
            [Permissions.Identity.RolesManage]);
    }

    [Fact]
    public async Task ShouldReturn200_WhenRequestIsValid()
    {
        // Arrange
        var roleId = await UserDataSeeder.SeedRoleWithPermissionsAsync(
            RoleManager, "Pesquisador", [Permissions.Research.ProjectsView]);

        var request = new UpdateRolePermissionsRequest(
            [Permissions.Research.PublicationsView, Permissions.Research.PartnersView]);

        // Act
        var response = await Client.PutAsJsonAsync($"/api/identity/roles/{roleId}/permissions", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var roles = await Client.GetFromJsonAsync<List<RoleDetailViewModel>>("/api/identity/roles");
        var role = roles!.Single(r => r.Id == roleId);
        Assert.Equal(
            new[] { Permissions.Research.PublicationsView, Permissions.Research.PartnersView }.OrderBy(p => p),
            role.Permissions.OrderBy(p => p));
    }

    [Fact]
    public async Task ShouldReturn200_AndClearPermissions_WhenRequestHasEmptyList()
    {
        // Arrange
        var roleId = await UserDataSeeder.SeedRoleWithPermissionsAsync(
            RoleManager, "Pesquisador", [Permissions.Research.ProjectsView]);

        var request = new UpdateRolePermissionsRequest([]);

        // Act
        var response = await Client.PutAsJsonAsync($"/api/identity/roles/{roleId}/permissions", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var roles = await Client.GetFromJsonAsync<List<RoleDetailViewModel>>("/api/identity/roles");
        var role = roles!.Single(r => r.Id == roleId);
        Assert.Empty(role.Permissions);
    }

    [Fact]
    public async Task ShouldReturn404_WhenRoleDoesNotExist()
    {
        // Arrange
        var request = new UpdateRolePermissionsRequest([Permissions.Research.ProjectsView]);

        // Act
        var response = await Client.PutAsJsonAsync($"/api/identity/roles/{Guid.NewGuid()}/permissions", request);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ShouldReturn400_WhenPermissionIsInvalid()
    {
        // Arrange
        var roleId = await UserDataSeeder.SeedRoleWithPermissionsAsync(
            RoleManager, "Pesquisador", [Permissions.Research.ProjectsView]);

        var request = new UpdateRolePermissionsRequest(["Invalid.Permission"]);

        // Act
        var response = await Client.PutAsJsonAsync($"/api/identity/roles/{roleId}/permissions", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ShouldReturn401_WhenNotAuthenticated()
    {
        // Arrange
        ClearAuthentication();
        var request = new UpdateRolePermissionsRequest([Permissions.Research.ProjectsView]);

        // Act
        var response = await Client.PutAsJsonAsync($"/api/identity/roles/{Guid.NewGuid()}/permissions", request);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
