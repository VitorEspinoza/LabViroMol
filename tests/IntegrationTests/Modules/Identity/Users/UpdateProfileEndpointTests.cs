using System.Net;
using System.Net.Http.Json;
using LabViroMol.Modules.Identity.Application.Users.ViewModels;
using LabViroMol.Modules.Identity.Contracts;
using LabViroMol.Modules.Identity.Presentation.Users;

namespace LabViroMol.Modules.Identity.IntegrationTests.Users;

public class UpdateProfileTests : UserEndpointsTestBase
{
    public UpdateProfileTests(IdentityIntegrationTestWebAppFactory factory) : base(factory) { }

    [Fact]
    public async Task ShouldReturn200_WhenRequestIsValid()
    {
        // Arrange
        var (userId, email) = await SeedBasicUser();
        AuthenticateAs(userId, email, "Basic", "User");

        var request = new UpdateProfileRequest(
            new UserInfo("Novo", "Nome", "123456", null, null, null));

        // Act
        var response = await Client.PutAsJsonAsync($"{BaseRoute}/me", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ShouldReturn400_WhenFirstNameIsEmpty()
    {
        // Arrange
        var (userId, email) = await SeedBasicUser();
        AuthenticateAs(userId, email, "Basic", "User");

        var request = new UpdateProfileRequest(
            new UserInfo("", "Nome", null, null, null, null));

        // Act
        var response = await Client.PutAsJsonAsync($"{BaseRoute}/me", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ShouldPersistEmergencyContact_WhenBothNameAndNumberAreProvided()
    {
        // Arrange
        var (userId, email) = await SeedBasicUser();
        AuthenticateAs(userId, email, "Basic", "User");

        var request = new UpdateProfileRequest(
            new UserInfo("Novo", "Nome", null, "Maria", "11999999999", null));

        // Act
        var response = await Client.PutAsJsonAsync($"{BaseRoute}/me", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var profile = await Client.GetFromJsonAsync<UserProfileViewModel>($"{BaseRoute}/me");
        Assert.Equal("Maria", profile!.UserData.EmergencyContactName);
        Assert.Equal("11999999999", profile.UserData.EmergencyContactNumber);
    }

    [Fact]
    public async Task ShouldPersistNullEmergencyContact_WhenBothNameAndNumberAreNull()
    {
        // Arrange
        var (userId, email) = await SeedBasicUser();
        AuthenticateAs(userId, email, "Basic", "User");

        await Client.PutAsJsonAsync($"{BaseRoute}/me",
            new UpdateProfileRequest(new UserInfo("Novo", "Nome", null, "Maria", "11999999999", null)));

        var request = new UpdateProfileRequest(
            new UserInfo("Novo", "Nome", null, null, null, null));

        // Act
        var response = await Client.PutAsJsonAsync($"{BaseRoute}/me", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var profile = await Client.GetFromJsonAsync<UserProfileViewModel>($"{BaseRoute}/me");
        Assert.Null(profile!.UserData.EmergencyContactName);
        Assert.Null(profile.UserData.EmergencyContactNumber);
    }
}
