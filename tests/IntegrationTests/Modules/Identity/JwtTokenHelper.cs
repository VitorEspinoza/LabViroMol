using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace LabViroMol.Modules.Identity.IntegrationTests;

public static class JwtTokenHelper
{
    public const string TestKey = "IntegrationTestSecretKeyThatIsLongEnoughForHmacSha256!!";
    public const string TestIssuer = "TestIssuer";
    public const string TestAudience = "TestAudience";

    public static string GenerateToken(
        Guid userId,
        string email,
        string firstName,
        string lastName,
        IEnumerable<string>? roles = null,
        IEnumerable<string>? permissions = null)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new(JwtRegisteredClaimNames.Email, email),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(ClaimTypes.GivenName, firstName),
            new(ClaimTypes.Surname, lastName)
        };

        if (roles is not null)
        {
            foreach (var role in roles)
                claims.Add(new Claim(ClaimTypes.Role, role));
        }

        if (permissions is not null)
        {
            foreach (var permission in permissions)
                claims.Add(new Claim("permission", permission));
        }

        var token = new JwtSecurityToken(
            issuer: TestIssuer,
            audience: TestAudience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(2),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
