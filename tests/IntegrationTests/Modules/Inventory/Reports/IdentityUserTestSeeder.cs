using LabViroMol.Modules.Identity.Domain.Users;
using LabViroMol.Modules.Identity.Infrastructure.Persistence;
using LabViroMol.Modules.Shared.Kernel.Identity;

namespace LabViroMol.Modules.Inventory.IntegrationTests.Reports;

public static class IdentityUserTestSeeder
{
    public static async Task<Guid> SeedUserAsync(
        LabViroMolIdentityDbContext dbContext,
        Guid userId,
        string firstName = "Maria",
        string lastName = "Auditora")
    {
        var user = User.Create(
            UserId.From(userId),
            new UserName(firstName, lastName),
            new Email($"{Guid.NewGuid():N}@labviromol-test.com"),
            null,
            null);

        await dbContext.DomainUsers.AddAsync(user);
        await dbContext.SaveChangesAsync();

        return userId;
    }
}
