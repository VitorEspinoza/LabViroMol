using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using LabViroMol.Modules.Identity.Domain.Users;
using LabViroMol.Modules.Identity.Infrastructure.Identity;
using LabViroMol.Modules.Identity.Infrastructure.Persistence;
using LabViroMol.Modules.Shared.Kernel.Identity;
using Microsoft.AspNetCore.Identity;

namespace LabViroMol.Modules.Identity.IntegrationTests.Users;

public static class UserDataSeeder
{
    public static async Task<(Guid UserId, string Email)> SeedUserWithPasswordAsync(
        LabViroMolIdentityDbContext dbContext,
        UserManager<ApplicationUser> userManager,
        string email,
        string password,
        string firstName,
        string lastName)
    {
        var appUser = new ApplicationUser
        {
            UserName = email,
            Email = email
        };

        await userManager.CreateAsync(appUser, password);

        var domainUser = User.Create(
            UserId.From(appUser.Id),
            new UserName(firstName, lastName),
            new Email(email),
            null,
            null);

        await dbContext.DomainUsers.AddAsync(domainUser);
        await dbContext.SaveChangesAsync();

        return (appUser.Id, email);
    }

    public static async Task<Guid> SeedRoleWithPermissionsAsync(
        RoleManager<ApplicationRole> roleManager,
        string name,
        List<string> permissions)
    {
        var role = new ApplicationRole { Name = name };
        await roleManager.CreateAsync(role);

        foreach (var permission in permissions)
            await roleManager.AddClaimAsync(role, new Claim("permission", permission));

        return role.Id;
    }

    public static async Task AssignRoleAsync(
        UserManager<ApplicationUser> userManager,
        Guid userId,
        string roleName)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        await userManager.AddToRoleAsync(user!, roleName);
    }
}
