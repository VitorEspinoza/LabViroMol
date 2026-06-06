using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using LabViroMol.Modules.Research.Infrastructure.Persistence;
using LabViroMol.Modules.Shared.Kernel.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace LabViroMol.Modules.Research.IntegrationTests;

[CollectionDefinition("ResearchIntegrationTests")]
public class ResearchIntegrationTestCollection : ICollectionFixture<ResearchIntegrationTestWebAppFactory> { }

[Collection("ResearchIntegrationTests")]
public abstract class BaseIntegrationTest : IDisposable
{
    protected readonly HttpClient Client;
    private readonly IServiceScope _scope;
    protected readonly ResearchDbContext DbContext;

    protected BaseIntegrationTest(ResearchIntegrationTestWebAppFactory factory)
    {
        Client = factory.CreateClient();
        _scope = factory.Services.CreateScope();
        DbContext = _scope.ServiceProvider.GetRequiredService<ResearchDbContext>();
        AuthenticateAsAdmin();
    }

    protected void AuthenticateAsAdmin()
    {
        var permissions = new[]
        {
            Permissions.Research.ProjectsView, Permissions.Research.ProjectsManage,
            Permissions.Research.PublicationsView, Permissions.Research.PublicationsManage,
            Permissions.Research.ResearchersView, Permissions.Research.ResearchersManage,
            Permissions.Research.PartnersView, Permissions.Research.PartnersManage,
            Permissions.Research.PositionsView, Permissions.Research.PositionsManage,
        };
        var token = GenerateTestToken(permissions);
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    protected void ClearAuthentication() =>
        Client.DefaultRequestHeaders.Authorization = null;

    private static string GenerateTestToken(IEnumerable<string> permissions)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("IntegrationTestSecretKeyThatIsLongEnoughForHmacSha256!!"));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Email, "admin@test.com"),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(ClaimTypes.GivenName, "Admin"),
            new(ClaimTypes.Surname, "Test"),
        };
        claims.AddRange(permissions.Select(p => new Claim("permission", p)));
        var token = new JwtSecurityToken(
            issuer: "TestIssuer",
            audience: "TestAudience",
            claims: claims,
            expires: DateTime.UtcNow.AddHours(2),
            signingCredentials: credentials);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public void Dispose()
    {
        DbContext.Database.EnsureDeleted();
        DbContext.Database.EnsureCreated();
        _scope.Dispose();
    }
}
