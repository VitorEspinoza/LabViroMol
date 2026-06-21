using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace LabViroMol.IntegrationTests.Shared;

public static class TestJwt
{
    public const string Key = "IntegrationTestSecretKeyThatIsLongEnoughForHmacSha256!!";
    public const string Issuer = "TestIssuer";
    public const string Audience = "TestAudience";

    public static string Generate(
        IEnumerable<string>? permissions = null,
        Guid? userId = null,
        string email = "admin@test.com",
        string firstName = "Admin",
        string lastName = "Test")
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Key));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, (userId ?? Guid.NewGuid()).ToString()),
            new(JwtRegisteredClaimNames.Email, email),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(ClaimTypes.GivenName, firstName),
            new(ClaimTypes.Surname, lastName),
        };

        if (permissions is not null)
            claims.AddRange(permissions.Select(p => new Claim("permission", p)));

        var token = new JwtSecurityToken(
            issuer: Issuer,
            audience: Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(2),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
