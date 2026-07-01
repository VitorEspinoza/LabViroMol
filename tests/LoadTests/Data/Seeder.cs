using System.Reflection;
using System.Security.Claims;
using Bogus;
using LabViroMol.LoadTests.Infrastructure;
using LabViroMol.Modules.Assets.Domain.Equipments;
using LabViroMol.Modules.Assets.Infrastructure.Persistence;
using LabViroMol.Modules.Identity.Domain.Users;
using LabViroMol.Modules.Identity.Infrastructure.Identity;
using LabViroMol.Modules.Identity.Infrastructure.Persistence;
using LabViroMol.Modules.Inventory.Domain.MaterialTypes;
using LabViroMol.Modules.Inventory.Infrastructure.Persistence;
using LabViroMol.Modules.Research.Domain.Partners;
using LabViroMol.Modules.Research.Domain.Positions;
using LabViroMol.Modules.Research.Domain.Projects;
using LabViroMol.Modules.Research.Domain.Researchers;
using LabViroMol.Modules.Research.Infrastructure.Persistence;
using LabViroMol.Modules.Scheduling.Domain.Schedules;
using LabViroMol.Modules.Scheduling.Infrastructure.Persistence;
using LabViroMol.Modules.Shared.Kernel.Authorization;
using LabViroMol.Modules.Shared.Kernel.Identity;
using LabViroMol.Modules.Shared.Kernel.Primitives;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using SchedulingPeriod = LabViroMol.Modules.Scheduling.Domain.Schedules.Scheduling;
using Unit = LabViroMol.Modules.Inventory.Domain.Materials.Unit;
using Material = LabViroMol.Modules.Inventory.Domain.Materials.Material;

namespace LabViroMol.LoadTests.Data;

public static class Seeder
{
    public static async Task SeedAsync(LoadTestConfig config, CancellationToken ct)
    {
        var catalogPath = ResolveCatalogPath(config);

        var identityOptions = BuildOptions<LabViroMolIdentityDbContext>(config.RequireConnectionString());
        var inventoryOptions = BuildOptions<InventoryDbContext>(config.RequireConnectionString());
        var researchOptions = BuildOptions<ResearchDbContext>(config.RequireConnectionString());
        var schedulingOptions = BuildOptions<SchedulingDbContext>(config.RequireConnectionString());
        var assetsOptions = BuildOptions<AssetsDbContext>(config.RequireConnectionString());

        await using var identityDb = new LabViroMolIdentityDbContext(identityOptions);
        await using var inventoryDb = new InventoryDbContext(inventoryOptions);
        await using var researchDb = new ResearchDbContext(researchOptions);
        await using var schedulingDb = new SchedulingDbContext(schedulingOptions);
        await using var assetsDb = new AssetsDbContext(assetsOptions);

        var catalog = new SeedCatalog
        {
            AuthUserEmails = [],
            MaterialTypeIds = [],
            EquipmentIds = [],
            PendingScheduleIds = [],
            ApprovedScheduleIds = [],
            ProjectTargets = [],
            ResearcherCandidateIds = []
        };

        await SeedIdentityAsync(identityDb, config, catalog, ct);
        await SeedInventoryAsync(inventoryDb, config, catalog, ct);
        await SeedAssetsAsync(assetsDb, config, catalog, ct);
        await SeedResearchAsync(researchDb, config, catalog, ct);
        await SeedSchedulesAsync(schedulingDb, config, catalog, ct);

        catalog.Save(catalogPath);
    }

    public static async Task<List<Guid>> AppendPendingSchedulesAsync(LoadTestConfig config, int count, CancellationToken ct)
    {
        var schedulingOptions = BuildOptions<SchedulingDbContext>(config.RequireConnectionString());
        await using var schedulingDb = new SchedulingDbContext(schedulingOptions);

        var equipmentIds = await BuildOptions<AssetsDbContext>(config.RequireConnectionString())
            .Let(static async (options, token) =>
            {
                await using var db = new AssetsDbContext(options);
                return await db.Equipments.AsNoTracking().Select(x => x.Id.Value).Take(250).ToListAsync(token);
            }, ct);

        var newIds = new List<Guid>(count);
        var faker = new Faker("pt_BR");
        var offset = await GetNextPendingScheduleOffsetAsync(schedulingDb, ct);

        for (var i = 0; i < count; i++)
        {
            var (date, start, end) = NextBusinessSlot(offset + i);
            var schedulingResult = SchedulingPeriod.Create(date, start, end);
            if (schedulingResult.IsFailure)
                throw new InvalidOperationException($"AppendPendingSchedules: slot inválido no offset {offset + i}: {string.Join(", ", schedulingResult.Errors)}");

            var equipmentId = equipmentIds[i % equipmentIds.Count];
            var scheduleResult = Schedule.Create(
                new Scheduler(faker.Name.FullName(), "Biomedicina", faker.Internet.Email()),
                schedulingResult.Data!,
                acceptTerm: true,
                advisorProfessor: $"Prof. {faker.Name.LastName()}",
                projectTitle: $"Projeto {faker.Random.Word()}",
                description: faker.Lorem.Sentence(8),
                equipments: [new ScheduleEquipment(equipmentId, $"Equip-{equipmentId:N}")]);
            if (scheduleResult.IsFailure)
                throw new InvalidOperationException($"AppendPendingSchedules: schedule inválido no offset {offset + i}: {string.Join(", ", scheduleResult.Errors)}");

            newIds.Add(scheduleResult.Data!.Id.Value);
            schedulingDb.Schedules.Add(scheduleResult.Data!);
        }

        await schedulingDb.SaveChangesAsync(ct);
        return newIds;
    }

    private static async Task SeedIdentityAsync(
        LabViroMolIdentityDbContext db,
        LoadTestConfig config,
        SeedCatalog catalog,
        CancellationToken ct)
    {
        var role = new ApplicationRole
        {
            Id = Guid.NewGuid(),
            Name = config.Auth.RoleName,
            NormalizedName = config.Auth.RoleName.ToUpperInvariant(),
            ConcurrencyStamp = Guid.NewGuid().ToString()
        };

        db.Roles.Add(role);

        foreach (var permission in GetAllPermissions())
        {
            db.RoleClaims.Add(new IdentityRoleClaim<Guid>
            {
                RoleId = role.Id,
                ClaimType = "permission",
                ClaimValue = permission
            });
        }

        var passwordHasher = new PasswordHasher<ApplicationUser>();

        for (var i = 0; i < config.Auth.UserCount; i++)
        {
            var email = $"{config.Auth.EmailPrefix}{i + 1}@test.local";
            var appUser = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                UserName = email,
                NormalizedUserName = email.ToUpperInvariant(),
                Email = email,
                NormalizedEmail = email.ToUpperInvariant(),
                EmailConfirmed = true,
                SecurityStamp = Guid.NewGuid().ToString(),
                ConcurrencyStamp = Guid.NewGuid().ToString()
            };

            appUser.PasswordHash = passwordHasher.HashPassword(appUser, config.Auth.Password);

            db.Users.Add(appUser);
            db.UserRoles.Add(new IdentityUserRole<Guid> { UserId = appUser.Id, RoleId = role.Id });
            db.DomainUsers.Add(User.Create(
                UserId.From(appUser.Id),
                new UserName("Load", $"Tester{i + 1}"),
                new Email(email),
                null,
                null));

            catalog.AuthUserEmails.Add(email);
        }

        await db.SaveChangesAsync(ct);
    }

    private static async Task SeedInventoryAsync(
        InventoryDbContext db,
        LoadTestConfig config,
        SeedCatalog catalog,
        CancellationToken ct)
    {
        var faker = new Faker("pt_BR");
        var materialTypes = Enumerable.Range(1, 24)
            .Select(i => MaterialType.Create($"Tipo {i:00}"))
            .ToList();

        db.ChangeTracker.AutoDetectChangesEnabled = false;
        db.MaterialTypes.AddRange(materialTypes);
        await db.SaveChangesAsync(ct);
        db.ChangeTracker.Clear();

        catalog.MaterialTypeIds.AddRange(materialTypes.Select(x => x.Id.Value));

        for (var batchStart = 0; batchStart < config.Data.Materials; batchStart += config.Data.Batches)
        {
            var batchSize = Math.Min(config.Data.Batches, config.Data.Materials - batchStart);

            for (var i = 0; i < batchSize; i++)
            {
                var type = materialTypes[(batchStart + i) % materialTypes.Count];
                var minStock = faker.Random.Decimal(10, 250);
                var stock = (batchStart + i) % 10 == 0
                    ? faker.Random.Decimal(0, minStock)
                    : faker.Random.Decimal(minStock, minStock * 5);

                var material = Material.Create(
                    $"{faker.Commerce.ProductName()}-{batchStart + i + 1:00000}",
                    $"Sala {faker.Random.Int(1, 12)}",
                    new Quantity(minStock),
                    new Quantity(stock),
                    faker.PickRandom<Unit>(),
                    type).Data!;

                db.Materials.Add(material);
            }

            await db.SaveChangesAsync(ct);
            db.ChangeTracker.Clear();
        }

        db.ChangeTracker.AutoDetectChangesEnabled = true;
    }

    private static async Task SeedAssetsAsync(
        AssetsDbContext db,
        LoadTestConfig config,
        SeedCatalog catalog,
        CancellationToken ct)
    {
        var faker = new Faker("pt_BR");
        db.ChangeTracker.AutoDetectChangesEnabled = false;

        for (var batchStart = 0; batchStart < config.Data.Equipments; batchStart += config.Data.Batches)
        {
            var batchSize = Math.Min(config.Data.Batches, config.Data.Equipments - batchStart);

            for (var i = 0; i < batchSize; i++)
            {
                var equipment = Equipment.Create(
                    $"{faker.Commerce.ProductAdjective()} {faker.Commerce.Product()}",
                    faker.Company.CompanyName(),
                    $"Modelo-{batchStart + i + 1:00000}",
                    $"EQ-{batchStart + i + 1:00000}",
                    faker.Lorem.Sentence(6),
                    $"Laboratório {faker.Random.Int(1, 8)}").Data!;

                catalog.EquipmentIds.Add(equipment.Id.Value);
                db.Equipments.Add(equipment);
            }

            await db.SaveChangesAsync(ct);
            db.ChangeTracker.Clear();
        }

        db.ChangeTracker.AutoDetectChangesEnabled = true;
    }

    private static async Task SeedResearchAsync(
        ResearchDbContext db,
        LoadTestConfig config,
        SeedCatalog catalog,
        CancellationToken ct)
    {
        var faker = new Faker("pt_BR");
        db.ChangeTracker.AutoDetectChangesEnabled = false;

        var positions = Enumerable.Range(1, 12)
            .Select(i => Position.Create($"Cargo {i:00}", $"Descrição do cargo {i:00}").Data!)
            .ToList();

        var partners = Enumerable.Range(1, 64)
            .Select(i => Partner.Create($"Parceiro {i:00}", faker.Lorem.Sentence(6)).Data!)
            .ToList();

        db.Positions.AddRange(positions);
        db.Partners.AddRange(partners);
        await db.SaveChangesAsync(ct);
        db.ChangeTracker.Clear();

        var candidateCount = Math.Max(config.Data.Projects * 2, 4000);
        var totalResearchers = candidateCount + config.Data.Projects;

        var researchers = new List<Researcher>(totalResearchers);

        for (var i = 0; i < totalResearchers; i++)
        {
            var researcherId = Guid.NewGuid();
            var position = positions[i % positions.Count];

            var researcher = Researcher.Create(
                ResearcherId.From(researcherId),
                new ResearcherName(faker.Name.FirstName(), faker.Name.LastName(), null, null),
                null,
                new AcademicBackground(DegreeLevel.Doctorate, faker.PickRandom("Virologia", "Bioquímica", "Farmácia", "Biologia")),
                position.Id);

            researchers.Add(researcher);
            if (i < candidateCount)
                catalog.ResearcherCandidateIds.Add(researcherId);

            db.Researchers.Add(researcher);

            if ((i + 1) % config.Data.Batches == 0)
            {
                await db.SaveChangesAsync(ct);
                db.ChangeTracker.Clear();
            }
        }

        await db.SaveChangesAsync(ct);
        db.ChangeTracker.Clear();

        for (var i = 0; i < config.Data.Projects; i++)
        {
            var lead = researchers[candidateCount + i];
            var partner = partners[i % partners.Count];

            var project = Project.Create(lead.Id, $"Projeto {i + 1:00000}", faker.Lorem.Sentence(12), partner.Id).Data!;

            if (i % 3 != 0)
                project.Start(lead.Id);

            db.Projects.Add(project);
            catalog.ProjectTargets.Add(new ProjectWriteTarget
            {
                ProjectId = project.Id.Value,
                LeadResearcherId = lead.Id.Value
            });

            if ((i + 1) % config.Data.Batches == 0)
            {
                await db.SaveChangesAsync(ct);
                db.ChangeTracker.Clear();
            }
        }

        await db.SaveChangesAsync(ct);
        db.ChangeTracker.Clear();
        db.ChangeTracker.AutoDetectChangesEnabled = true;
    }

    private static async Task SeedSchedulesAsync(
        SchedulingDbContext db,
        LoadTestConfig config,
        SeedCatalog catalog,
        CancellationToken ct)
    {
        var faker = new Faker("pt_BR");
        db.ChangeTracker.AutoDetectChangesEnabled = false;

        var total = config.Data.SchedulesPending + config.Data.SchedulesApproved;
        for (var i = 0; i < total; i++)
        {
            var (date, start, end) = NextBusinessSlot(i + 1);
            var scheduling = SchedulingPeriod.Create(date, start, end).Data!;
            var equipmentId = catalog.EquipmentIds[i % catalog.EquipmentIds.Count];

            var schedule = Schedule.Create(
                new Scheduler(faker.Name.FullName(), "Biomedicina", faker.Internet.Email()),
                scheduling,
                acceptTerm: true,
                advisorProfessor: $"Prof. {faker.Name.LastName()}",
                projectTitle: $"Projeto agenda {i + 1:00000}",
                description: faker.Lorem.Sentence(8),
                equipments: [new ScheduleEquipment(equipmentId, $"Equip-{equipmentId:N}")]).Data!;

            if (i >= config.Data.SchedulesPending)
            {
                schedule.Approve(UserId.From(Guid.NewGuid()));
                catalog.ApprovedScheduleIds.Add(schedule.Id.Value);
            }
            else
            {
                catalog.PendingScheduleIds.Add(schedule.Id.Value);
            }

            db.Schedules.Add(schedule);

            if ((i + 1) % config.Data.Batches == 0)
            {
                await db.SaveChangesAsync(ct);
                db.ChangeTracker.Clear();
            }
        }

        await db.SaveChangesAsync(ct);
        db.ChangeTracker.AutoDetectChangesEnabled = true;
    }

    private static DbContextOptions<TDbContext> BuildOptions<TDbContext>(string connectionString)
        where TDbContext : DbContext
    {
        return new DbContextOptionsBuilder<TDbContext>()
            .UseNpgsql(connectionString)
            .Options;
    }

    private static IEnumerable<string> GetAllPermissions()
    {
        return typeof(Permissions)
            .GetNestedTypes(BindingFlags.Public)
            .SelectMany(x => x.GetFields(BindingFlags.Public | BindingFlags.Static))
            .Where(x => x.IsLiteral && x.FieldType == typeof(string))
            .Select(x => (string)x.GetRawConstantValue()!)
            .Distinct()
            .Where(x => x.Contains('.'));
    }

    private static string ResolveCatalogPath(LoadTestConfig config)
    {
        return Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, config.Data.SeedCatalogPath));
    }

    private static (DateOnly Date, DateTimeOffset Start, DateTimeOffset End) NextBusinessSlot(int offset)
    {
        var date = DateOnly.FromDateTime(DateTime.Today);
        var remaining = Math.Max(offset, 1);

        while (remaining > 0)
        {
            date = date.AddDays(1);
            if (date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
                continue;

            remaining--;
        }

        var start = new DateTimeOffset(date.ToDateTime(new TimeOnly(9 + (offset % 6), 0)), TimeSpan.Zero);
        var end = start.AddHours(1);
        return (date, start, end);
    }

    private static async Task<int> GetNextPendingScheduleOffsetAsync(SchedulingDbContext db, CancellationToken ct)
    {
        var totalSchedules = await db.Schedules.AsNoTracking().CountAsync(ct);
        return totalSchedules + 1;
    }

    private static async Task<TResult> Let<T, TResult>(this T value, Func<T, CancellationToken, Task<TResult>> transform, CancellationToken ct) =>
        await transform(value, ct);
}
