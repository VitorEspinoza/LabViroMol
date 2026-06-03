using System.Net;
using System.Threading.Tasks;
using Xunit;

namespace LabViroMol.Modules.Identity.IntegrationTests.Users;

public class GetAllUsersTests : UserEndpointsTestBase
{
    public GetAllUsersTests(IdentityIntegrationTestWebAppFactory factory) : base(factory) { }

    [Fact]
    public async Task ShouldReturn200_WhenAuthorized()
    {
        // Arrange
        await SeedAuthenticatedAdmin();
        await SeedBasicUser();

        // Act
        var response = await Client.GetAsync(BaseRoute);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}

public class GetUserByIdTests : UserEndpointsTestBase
{
    public GetUserByIdTests(IdentityIntegrationTestWebAppFactory factory) : base(factory) { }

    [Fact]
    public async Task ShouldReturn200_WhenUserExists()
    {
        // Arrange
        await SeedAuthenticatedAdmin();
        var (targetId, _) = await SeedBasicUser();

        // Act
        var response = await Client.GetAsync($"{BaseRoute}/{targetId}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}

public class GetMyProfileTests : UserEndpointsTestBase
{
    public GetMyProfileTests(IdentityIntegrationTestWebAppFactory factory) : base(factory) { }

    [Fact]
    public async Task ShouldReturn200_WhenAuthenticated()
    {
        // Arrange
        var (userId, email) = await SeedBasicUser();
        AuthenticateAs(userId, email, "Basic", "User");

        // Act
        var response = await Client.GetAsync($"{BaseRoute}/me");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
