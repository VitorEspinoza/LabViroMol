using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using LabViroMol.Modules.Inventory.Domain.Materials;
using LabViroMol.Modules.Inventory.Domain.MaterialTypes;
using LabViroMol.Modules.Inventory.Infrastructure.Persistence;
using LabViroMol.Modules.Shared.Kernel.Authorization;
using LabViroMol.Modules.Shared.Kernel.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace LabViroMol.Modules.Inventory.IntegrationTests;


[CollectionDefinition("IntegrationTests")]
public class IntegrationTestCollection : ICollectionFixture<IntegrationTestWebAppFactory> { }

[Collection("IntegrationTests")]
public abstract class BaseIntegrationTest : IDisposable
{
    protected readonly HttpClient Client;
    private readonly IServiceScope _scope;

    protected readonly InventoryDbContext DbContext;

    protected BaseIntegrationTest(IntegrationTestWebAppFactory factory)
    {
        Client = factory.CreateClient();
        _scope = factory.Services.CreateScope();
        DbContext = _scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
        AuthenticateAsAdmin();
    }

    protected void AuthenticateAsAdmin()
    {
        var permissions = new[]
        {
            Permissions.Inventory.MaterialsView, Permissions.Inventory.MaterialsManage,
            Permissions.Inventory.KitsView, Permissions.Inventory.KitsManage,
            Permissions.Inventory.OrdersView, Permissions.Inventory.OrdersManage,
            Permissions.Inventory.StockView, Permissions.Inventory.StockManage,
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