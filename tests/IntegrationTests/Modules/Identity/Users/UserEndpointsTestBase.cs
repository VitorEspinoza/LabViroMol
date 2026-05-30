using LabViroMol.Modules.Shared.Kernel.Authorization;

namespace LabViroMol.Modules.Identity.IntegrationTests.Users;

public abstract class UserEndpointsTestBase : BaseIdentityIntegrationTest
{
    protected const string BaseRoute = "/api/identity/users";

    protected UserEndpointsTestBase(IdentityIntegrationTestWebAppFactory factory) : base(factory) { }

    protected async Task<(Guid UserId, string Email)> SeedAuthenticatedAdmin()
    {
        var (userId, email) = await UserDataSeeder.SeedUserWithPasswordAsync(
            DbContext, UserManager,
            "admin@labviromol.com", "Admin@123456",
            "Admin", "User");

        await UserDataSeeder.SeedRoleWithPermissionsAsync(
            RoleManager, "Admin",
            [Permissions.Identity.UsersManage, Permissions.Identity.UsersView]);

        await UserDataSeeder.AssignRoleAsync(UserManager, userId, "Admin");

        AuthenticateAs(userId, email, "Admin", "User",
            [Permissions.Identity.UsersManage, Permissions.Identity.UsersView]);

        return (userId, email);
    }

    protected async Task<(Guid UserId, string Email)> SeedBasicUser(
        string email = "user@labviromol.com",
        string password = "User@123456")
    {
        return await UserDataSeeder.SeedUserWithPasswordAsync(
            DbContext, UserManager,
            email, password,
            "Basic", "User");
    }
}
