using System.Net;
using System.Net.Http.Json;
using LabViroMol.Modules.Identity.Application.Roles.CreateRole;
using LabViroMol.Modules.Shared.Kernel.Authorization;

namespace LabViroMol.Modules.Identity.IntegrationTests.Roles;

public class CreateRoleTests : BaseIdentityIntegrationTest
{
    public CreateRoleTests(IdentityIntegrationTestWebAppFactory factory) : base(factory) { }

    [Fact]
    public async Task ShouldReturn201_WhenRequestIsValid()
    {
        // Arrange
        var command = new CreateRoleCommand("Pesquisador", [Permissions.Research.ProjectsView]);

        // Act
        var response = await Client.PostAsJsonAsync("/api/identity/roles", command);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task ShouldReturn400_WhenNameIsEmpty()
    {
        // Arrange
        var command = new CreateRoleCommand("", [Permissions.Research.ProjectsView]);

        // Act
        var response = await Client.PostAsJsonAsync("/api/identity/roles", command);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
