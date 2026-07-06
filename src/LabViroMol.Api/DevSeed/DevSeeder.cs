using System.Reflection;
using System.Security.Claims;
using LabViroMol.Modules.Assets.Domain.Equipments;
using LabViroMol.Modules.Assets.Domain.MaintenanceRequests;
using LabViroMol.Modules.Assets.Infrastructure.Persistence;
using LabViroMol.Modules.Identity.Domain.Users;
using LabViroMol.Modules.Identity.Infrastructure.Identity;
using LabViroMol.Modules.Identity.Infrastructure.Persistence;
using LabViroMol.Modules.Inventory.Domain.Kits;
using LabViroMol.Modules.Inventory.Domain.Materials;
using LabViroMol.Modules.Inventory.Domain.MaterialTypes;
using LabViroMol.Modules.Inventory.Domain.Orders;
using LabViroMol.Modules.Inventory.Infrastructure.Persistence;
using LabViroMol.Modules.Notify.Domain.Notifications;
using LabViroMol.Modules.Notify.Infrastructure.Persistence;
using LabViroMol.Modules.Research.Domain.Partners;
using LabViroMol.Modules.Research.Domain.Positions;
using LabViroMol.Modules.Research.Domain.Publications;
using LabViroMol.Modules.Research.Domain.Researchers;
using LabViroMol.Modules.Research.Infrastructure.Persistence;
using LabViroMol.Modules.Scheduling.Domain.Schedules;
using LabViroMol.Modules.Scheduling.Infrastructure.Persistence;
using LabViroMol.Modules.Shared.Kernel.Authorization;
using LabViroMol.Modules.Shared.Kernel.Identity;
using LabViroMol.Modules.Shared.Kernel.Interfaces;
using LabViroMol.Modules.Shared.Kernel.Primitives;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ResearchProject = LabViroMol.Modules.Research.Domain.Projects.Project;
using InventoryProjectId = LabViroMol.Modules.Inventory.Domain.References.ProjectId;

namespace LabViroMol.Api.DevSeed;

/// <summary>
/// Populates ~20 valid records per aggregate for local manual testing. Runs once, only in
/// Development, only when the database is empty (checked via Identity's Users table).
/// Uses each aggregate's own factory methods, so the data is exactly as valid as anything
/// created through the real API. Bypasses each module's IUnitOfWork/Mediator on purpose:
/// seeded data shouldn't trigger real domain-event side effects (emails, outbox messages).
/// </summary>
public static class DevSeeder
{
    private const string DevPassword = "Labviromol@123";
    private static readonly UserId SystemActor = UserId.From(Guid.Empty);

    public static async Task SeedAsync(IServiceProvider rootServices, IConfiguration configuration, ILogger logger)
    {
        if (!configuration.GetValue("DevSeed:Enabled", true))
        {
            logger.LogInformation("DevSeed: disabled via configuration, skipping");
            return;
        }

        using var scope = rootServices.CreateScope();
        var services = scope.ServiceProvider;

        var identityDb = services.GetRequiredService<LabViroMolIdentityDbContext>();

        if (await identityDb.Users.AnyAsync())
        {
            logger.LogInformation("DevSeed: data already present, skipping");
            return;
        }

        logger.LogInformation("DevSeed: seeding development data...");

        var users = await SeedIdentityAsync(services, identityDb);
        var equipment = await SeedAssetsAsync(services);
        var research = await SeedResearchAsync(services, users);
        var inventory = await SeedInventoryAsync(services, research.Projects);
        var schedules = await SeedSchedulingAsync(services, equipment, users);
        await SeedNotifyAsync(services, schedules, inventory.Orders, equipment.MaintenanceRequestIds);

        logger.LogInformation(
            "DevSeed: done. Log in with {Email} / {Password}",
            users[0].Email,
            DevPassword);
    }

    private static void StampCreationAudit(DbContext context)
    {
        var now = DateTimeOffset.UtcNow;

        foreach (var entry in context.ChangeTracker.Entries())
        {
            if (entry is { State: EntityState.Added, Entity: ICreationAuditable })
            {
                entry.Property("CreatedAt").CurrentValue = now;
                entry.Property("CreatedBy").CurrentValue = SystemActor;
            }
        }
    }

    private static string RemoveDiacritics(string value)
    {
        var normalized = value.Normalize(System.Text.NormalizationForm.FormD);
        var chars = normalized.Where(c =>
            System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c) !=
            System.Globalization.UnicodeCategory.NonSpacingMark);

        return new string(chars.ToArray()).Normalize(System.Text.NormalizationForm.FormC);
    }

    private static DateTimeOffset NextBusinessDateTime(int daysFromNow, int hour)
    {
        var date = DateOnly.FromDateTime(DateTime.Today).AddDays(daysFromNow);
        while (date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
            date = date.AddDays(1);

        return new DateTimeOffset(date.Year, date.Month, date.Day, hour, 0, 0, TimeSpan.Zero);
    }

    // ---------------------------------------------------------------------------------------
    // Identity: 1 Admin + 19 regular users, 2 roles (Admin with every permission, Pesquisador
    // with a research-oriented subset).
    // ---------------------------------------------------------------------------------------
    public sealed record SeededUser(Guid UserId, string Email, string FirstName, string LastName, bool IsAdmin);

    private static readonly string[] FirstNames =
    [
        "Ana", "Bruno", "Carla", "Daniel", "Eduarda", "Felipe", "Gabriela", "Heitor", "Isabela", "João",
        "Karina", "Lucas", "Marina", "Nicolas", "Olívia", "Pedro", "Renata", "Samuel", "Tatiane", "Vitor"
    ];

    private static readonly string[] LastNames =
    [
        "Silva", "Souza", "Oliveira", "Santos", "Pereira", "Costa", "Rodrigues", "Almeida", "Nascimento", "Lima",
        "Araújo", "Fernandes", "Carvalho", "Gomes", "Martins", "Rocha", "Ribeiro", "Barbosa", "Cardoso", "Teixeira"
    ];

    private static async Task<List<SeededUser>> SeedIdentityAsync(IServiceProvider services, LabViroMolIdentityDbContext db)
    {
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = services.GetRequiredService<RoleManager<ApplicationRole>>();

        var allPermissions = typeof(Permissions)
            .GetNestedTypes(BindingFlags.Public | BindingFlags.Static)
            .SelectMany(t => t.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy))
            .Where(f => f.IsLiteral && !f.IsInitOnly && f.FieldType == typeof(string))
            .Select(f => (string)f.GetRawConstantValue()!)
            .Where(p => p.Contains('.'))
            .Distinct()
            .ToList();

        var adminRole = new ApplicationRole { Name = "Admin" };
        await roleManager.CreateAsync(adminRole);
        foreach (var permission in allPermissions)
            await roleManager.AddClaimAsync(adminRole, new Claim("permission", permission));

        var researcherPermissions = allPermissions
            .Where(p => p.StartsWith("Research.") || p == Permissions.Inventory.OrdersManage)
            .ToList();
        var researcherRole = new ApplicationRole { Name = "Pesquisador" };
        await roleManager.CreateAsync(researcherRole);
        foreach (var permission in researcherPermissions)
            await roleManager.AddClaimAsync(researcherRole, new Claim("permission", permission));

        var result = new List<SeededUser>();

        for (var i = 0; i < 20; i++)
        {
            var isAdmin = i == 0;
            var firstName = isAdmin ? "Admin" : FirstNames[i];
            var lastName = isAdmin ? "LabViroMol" : LastNames[i];
            var email = isAdmin
                ? "admin@labviromol.local"
                : $"{RemoveDiacritics(firstName).ToLowerInvariant()}.{RemoveDiacritics(lastName).ToLowerInvariant()}@labviromol.local";

            var appUser = new ApplicationUser { UserName = email, Email = email, EmailConfirmed = true };
            var identityResult = await userManager.CreateAsync(appUser, DevPassword);
            if (!identityResult.Succeeded)
                continue;

            await userManager.AddToRoleAsync(appUser, isAdmin ? "Admin" : "Pesquisador");

            var domainUser = User.Create(
                UserId.From(appUser.Id),
                new UserName(firstName, lastName),
                new Email(email),
                phoneNumber: null,
                emergencyContact: null);

            db.DomainUsers.Add(domainUser);

            result.Add(new SeededUser(appUser.Id, email, firstName, lastName, isAdmin));
        }

        StampCreationAudit(db);
        await db.SaveChangesAsync();

        return result;
    }

    // ---------------------------------------------------------------------------------------
    // Assets: 20 Equipment + 20 MaintenanceRequest (varied statuses).
    // ---------------------------------------------------------------------------------------
    public sealed record AssetsSeedResult(List<EquipmentId> EquipmentIds, List<MaintenanceRequestId> MaintenanceRequestIds);

    private static readonly (string Name, string Brand, string Model)[] EquipmentCatalog =
    [
        ("Microscópio Óptico", "Zeiss", "Primo Star"),
        ("Centrífuga Refrigerada", "Eppendorf", "5430R"),
        ("Autoclave Vertical", "Phoenix", "AV-75"),
        ("Câmara de Fluxo Laminar", "Veco", "VLFS-12"),
        ("Espectrofotômetro UV-Vis", "Thermo Fisher", "Genesys 150"),
        ("Estufa Bacteriológica", "Odontobrás", "EL 202"),
        ("Freezer -80°C", "Panasonic", "MDF-U74V"),
        ("Geladeira de Laboratório", "Consul", "CRP34"),
        ("Agitador Orbital", "IKA", "KS 260"),
        ("Banho-Maria Digital", "Nova Ética", "300/6D"),
        ("Termociclador PCR", "Applied Biosystems", "Veriti 96"),
        ("Balança Analítica", "Shimadzu", "AUW220D"),
        ("Microscópio de Fluorescência", "Nikon", "Eclipse Ni-U"),
        ("Sistema de Eletroforese", "Bio-Rad", "PowerPac Basic"),
        ("pHmetro Digital", "Hanna", "HI2211"),
        ("Agitador Vórtex", "Biomixer", "QL-901"),
        ("Destilador de Água", "Quimis", "Q341-210"),
        ("Capela de Exaustão de Gases", "Permution", "CE-08"),
        ("Contador de Colônias", "Phoenix", "CP-608"),
        ("Sequenciador Genético", "Illumina", "MiSeq")
    ];

    private static async Task<AssetsSeedResult> SeedAssetsAsync(IServiceProvider services)
    {
        var db = services.GetRequiredService<AssetsDbContext>();

        var equipmentIds = new List<EquipmentId>();
        for (var i = 0; i < EquipmentCatalog.Length; i++)
        {
            var (name, brand, model) = EquipmentCatalog[i];
            var result = Equipment.Create(
                name: name,
                brand: brand,
                model: model,
                code: $"EQ-{i + 1:D3}",
                description: $"{name} para uso rotineiro do laboratório.",
                location: i % 2 == 0 ? "Laboratório Central" : "Laboratório de Apoio");

            if (result.IsFailure)
                continue;

            db.Equipments.Add(result.Data!);
            equipmentIds.Add(result.Data!.Id);
        }

        var maintenanceRequestIds = new List<MaintenanceRequestId>();
        var problems = new[]
        {
            "Equipamento não liga.", "Ruído incomum durante operação.", "Vazamento identificado.",
            "Calibração vencida.", "Falha intermitente de leitura."
        };

        for (var i = 0; i < 20; i++)
        {
            var equipmentId = equipmentIds[i % equipmentIds.Count];
            var result = MaintenanceRequest.Create(
                description: $"Solicitação de manutenção #{i + 1}",
                problemDescription: problems[i % problems.Length],
                equipmentId: equipmentId.Value);

            if (result.IsFailure)
                continue;

            var request = result.Data!;

            // Vary status across the 20 requests: mostly Requested, some In Progress/Done/Cancelled.
            switch (i % 4)
            {
                case 1:
                    request.Start();
                    break;
                case 2:
                    request.Start();
                    request.Done();
                    break;
                case 3:
                    request.Cancel();
                    break;
            }

            db.MaintenanceRequests.Add(request);
            maintenanceRequestIds.Add(request.Id);
        }

        StampCreationAudit(db);
        await db.SaveChangesAsync();

        return new AssetsSeedResult(equipmentIds, maintenanceRequestIds);
    }

    // ---------------------------------------------------------------------------------------
    // Research: 8 Positions, 10 Partners, 19 Researchers (1:1 with non-admin users),
    // 20 Projects, 20 Publications.
    // ---------------------------------------------------------------------------------------
    public sealed record ResearchSeedResult(List<ResearchProject> Projects);

    private static readonly string[] PositionCatalog =
    [
        "Professor Titular", "Professor Associado", "Professor Adjunto", "Pós-Doutorando",
        "Doutorando", "Mestrando", "Técnico de Laboratório", "Pesquisador Visitante"
    ];

    private static readonly string[] PartnerCatalog =
    [
        "Universidade Federal do Paraná", "Fundação Oswaldo Cruz", "Instituto Butantan",
        "Universidade de São Paulo", "Universidade Federal do Rio de Janeiro",
        "Instituto Carlos Chagas", "Hospital de Clínicas da UFPR", "Secretaria de Saúde do Paraná",
        "Ministério da Saúde", "Organização Mundial da Saúde"
    ];

    private static async Task<ResearchSeedResult> SeedResearchAsync(IServiceProvider services, List<SeededUser> users)
    {
        var db = services.GetRequiredService<ResearchDbContext>();

        var positions = new List<Position>();
        foreach (var name in PositionCatalog)
        {
            var result = Position.Create(name, $"Cargo/função: {name}.");
            if (result.IsFailure)
                continue;

            db.Positions.Add(result.Data!);
            positions.Add(result.Data!);
        }

        var partners = new List<Partner>();
        foreach (var name in PartnerCatalog)
        {
            var result = Partner.Create(name, $"Instituição parceira: {name}.");
            if (result.IsFailure)
                continue;

            db.Partners.Add(result.Data!);
            partners.Add(result.Data!);
        }

        var researchers = new List<Researcher>();
        var nonAdminUsers = users.Where(u => !u.IsAdmin).ToList();
        for (var i = 0; i < nonAdminUsers.Count; i++)
        {
            var user = nonAdminUsers[i];
            var position = positions[i % positions.Count];

            var researcher = Researcher.Create(
                ResearcherId.From(user.UserId),
                new ResearcherName(user.FirstName, user.LastName, citationName: null, displayName: null),
                lattesUrl: null,
                new AcademicBackground(
                    (DegreeLevel)(i % Enum.GetValues<DegreeLevel>().Length),
                    "Virologia e Microbiologia"),
                position.Id);

            db.Researchers.Add(researcher);
            researchers.Add(researcher);
        }

        var projects = new List<ResearchProject>();
        var projectTopics = new[]
        {
            "Vigilância genômica de vírus respiratórios", "Desenvolvimento de testes rápidos para arbovírus",
            "Resistência antiviral em cepas circulantes", "Epidemiologia molecular de patógenos emergentes",
            "Novas metodologias de diagnóstico por PCR", "Impacto de variantes virais na resposta imune"
        };

        for (var i = 0; i < 20; i++)
        {
            var lead = researchers[i % researchers.Count];
            var partner = partners[i % partners.Count];
            var topic = projectTopics[i % projectTopics.Length];

            var result = ResearchProject.Create(
                lead.Id,
                title: $"{topic} — Estudo {i + 1}",
                description: $"Projeto de pesquisa sobre {topic.ToLowerInvariant()}.",
                partner.Id);

            if (result.IsFailure)
                continue;

            var project = result.Data!;
            if (i % 3 == 1)
                project.Start(lead.Id);

            db.Projects.Add(project);
            projects.Add(project);
        }

        var publications = new List<Publication>();
        var journals = new[] { "Revista Brasileira de Virologia", "Journal of Clinical Microbiology", "The Lancet Microbe" };
        for (var i = 0; i < 20; i++)
        {
            var result = Publication.Create(
                title: $"Achados sobre {projectTopics[i % projectTopics.Length].ToLowerInvariant()} — artigo {i + 1}",
                description: "Publicação científica derivada das atividades do laboratório.",
                doi: $"10.1234/labviromol.{2026}.{i + 1:D4}",
                publicationDate: DateOnly.FromDateTime(DateTime.Today.AddDays(-30 * (i + 1))),
                publishedOn: journals[i % journals.Length],
                publishUrl: $"https://doi.org/10.1234/labviromol.{2026}.{i + 1:D4}");

            if (result.IsFailure)
                continue;

            var publication = result.Data!;
            publication.AddResearcher(researchers[i % researchers.Count].Id);
            if (researchers.Count > 1)
                publication.AddResearcher(researchers[(i + 1) % researchers.Count].Id);

            db.Publications.Add(publication);
            publications.Add(publication);
        }

        StampCreationAudit(db);
        await db.SaveChangesAsync();

        return new ResearchSeedResult(projects);
    }

    // ---------------------------------------------------------------------------------------
    // Inventory: 8 MaterialTypes, 20 Materials, 10 Kits, 20 Orders (soft-linked to real
    // Research projects, no DB FK).
    // ---------------------------------------------------------------------------------------
    public sealed record InventorySeedResult(List<OrderId> Orders);

    private static readonly string[] MaterialTypeCatalog =
    [
        "Reagente Químico", "Vidraria", "Equipamento de Proteção Individual", "Meio de Cultura",
        "Consumível Descartável", "Solvente", "Kit Diagnóstico", "Material de Biossegurança"
    ];

    private static readonly string[] MaterialCatalog =
    [
        "Etanol 70%", "Ágar Nutriente", "Soro Fisiológico", "Luvas de Nitrilo", "Álcool Isopropílico",
        "Meio LB", "Tubos Falcon 15ml", "Ponteiras 1000µL", "Placas de Petri", "PBS 1x",
        "Formaldeído 4%", "Ágar Sangue", "Kit de Extração de RNA", "Máscaras N95", "Papel Filtro",
        "Solução Salina Tamponada", "Corante de Gram", "Tampão TAE", "Reagente de Bradford", "Água Destilada"
    ];

    private static async Task<InventorySeedResult> SeedInventoryAsync(IServiceProvider services, List<ResearchProject> projects)
    {
        var db = services.GetRequiredService<InventoryDbContext>();

        var types = new List<MaterialType>();
        foreach (var name in MaterialTypeCatalog)
        {
            var type = MaterialType.Create(name);
            db.MaterialTypes.Add(type);
            types.Add(type);
        }

        var units = Enum.GetValues<Unit>();
        var materials = new List<Material>();
        for (var i = 0; i < MaterialCatalog.Length; i++)
        {
            var type = types[i % types.Count];
            var result = Material.Create(
                name: MaterialCatalog[i],
                location: i % 2 == 0 ? "Almoxarifado Central" : "Almoxarifado Anexo",
                minStock: new Quantity(10),
                stockQuantity: new Quantity(50 + i * 5),
                unit: units[i % units.Length],
                type: type);

            if (result.IsFailure)
                continue;

            db.Materials.Add(result.Data!);
            materials.Add(result.Data!);
        }

        var kitNames = new[]
        {
            "Kit Extração de DNA", "Kit PCR Convencional", "Kit Coloração de Gram", "Kit ELISA",
            "Kit Biossegurança Nível 2", "Kit Coleta de Amostras", "Kit Cultura Bacteriana",
            "Kit Diagnóstico Rápido", "Kit Preparo de Meios", "Kit Limpeza de Bancada"
        };

        for (var i = 0; i < kitNames.Length; i++)
        {
            var items = new List<KitItem>
            {
                new(materials[i % materials.Count].Id, new Quantity(2)),
                new(materials[(i + 1) % materials.Count].Id, new Quantity(1))
            };

            var kit = Kit.Create(kitNames[i], $"{kitNames[i]} com os materiais essenciais para a rotina.", items);
            db.Kits.Add(kit);
        }

        var orderIds = new List<OrderId>();
        for (var i = 0; i < 20; i++)
        {
            var material = materials[i % materials.Count];
            var project = projects[i % projects.Count];

            var order = Order.Create(
                material.Id,
                InventoryProjectId.From(project.Id.Value),
                new Quantity(5 + i % 10),
                $"Pedido de reposição de {material.Name.ToLowerInvariant()} para o projeto \"{project.Title}\".");

            // Vary status: some pending, some processed, some received, one canceled.
            if (i % 4 is 1 or 2)
                order.Process(SystemActor, "Sistema (dev seed)", "Processado automaticamente pelo seed de desenvolvimento.");
            if (i % 4 == 2)
                order.Receive(SystemActor, "Sistema (dev seed)", new Quantity(5 + i % 10), "Recebido conforme solicitado.");
            if (i % 4 == 3)
                order.Cancel();

            db.Orders.Add(order);
            orderIds.Add(order.Id);
        }

        StampCreationAudit(db);
        await db.SaveChangesAsync();

        return new InventorySeedResult(orderIds);
    }

    // ---------------------------------------------------------------------------------------
    // Scheduling: 20 Schedules across upcoming business days, varied statuses.
    // ---------------------------------------------------------------------------------------
    private static async Task<List<Schedule>> SeedSchedulingAsync(
        IServiceProvider services, AssetsSeedResult equipment, List<SeededUser> users)
    {
        var db = services.GetRequiredService<SchedulingDbContext>();
        var admin = users.First(u => u.IsAdmin);

        var courses = new[] { "Biomedicina", "Farmácia", "Medicina", "Biotecnologia", "Enfermagem" };
        var schedules = new List<Schedule>();

        for (var i = 0; i < 20; i++)
        {
            var dayOffset = 3 + i; // spread across upcoming business days
            var date = DateOnly.FromDateTime(NextBusinessDateTime(dayOffset, 9).Date);
            var start = NextBusinessDateTime(dayOffset, 9 + i % 4);
            var end = start.AddHours(2);

            var schedulingResult = Scheduling.Create(date, start, end);
            if (schedulingResult.IsFailure)
                continue;

            var scheduler = new Scheduler(
                $"{FirstNames[i % FirstNames.Length]} {LastNames[i % LastNames.Length]}",
                courses[i % courses.Length],
                $"aluno{i + 1}@estudante.ufpr.br");

            var equipmentForSchedule = new List<ScheduleEquipment>
            {
                new(equipment.EquipmentIds[i % equipment.EquipmentIds.Count].Value,
                    EquipmentCatalog[i % EquipmentCatalog.Length].Name)
            };

            var result = Schedule.Create(
                scheduler,
                schedulingResult.Data!,
                acceptTerm: true,
                advisorProfessor: $"Prof. {LastNames[(i + 3) % LastNames.Length]}",
                projectTitle: $"Aula prática de laboratório #{i + 1}",
                description: "Uso do laboratório para atividades práticas da disciplina.",
                equipmentForSchedule);

            if (result.IsFailure)
                continue;

            var schedule = result.Data!;

            switch (i % 4)
            {
                case 1:
                    schedule.Approve(UserId.From(admin.UserId));
                    break;
                case 2:
                    schedule.Refuse(UserId.From(admin.UserId), "Equipamento indisponível na data solicitada.");
                    break;
                case 3:
                    schedule.Approve(UserId.From(admin.UserId));
                    schedule.Cancel("Cancelado pelo solicitante.", UserId.From(admin.UserId));
                    break;
            }

            db.Schedules.Add(schedule);
            schedules.Add(schedule);
        }

        StampCreationAudit(db);
        await db.SaveChangesAsync();

        return schedules;
    }

    // ---------------------------------------------------------------------------------------
    // Notify: 20 synthetic notifications referencing real seeded entities, matching the same
    // conventions used by the real domain-event handlers (referenceModule/type/permission).
    // ---------------------------------------------------------------------------------------
    private static async Task SeedNotifyAsync(
        IServiceProvider services,
        List<Schedule> schedules,
        List<OrderId> orders,
        List<MaintenanceRequestId> maintenanceRequests)
    {
        var db = services.GetRequiredService<NotifyDbContext>();

        var pendingSchedules = schedules.Where(s => s.Status == ScheduleStatus.PENDING).ToList();
        var notifications = new List<Notification>();

        for (var i = 0; i < 20; i++)
        {
            Result<Notification> result = (i % 3) switch
            {
                0 when pendingSchedules.Count > 0 => Notification.Create(
                    "Agendamento solicitado",
                    "Novo agendamento aguardando aprovação.",
                    Permissions.Scheduling.SchedulesManage,
                    pendingSchedules[i % pendingSchedules.Count].Id.Value.ToString(),
                    "Schedule",
                    "NewSchedule"),

                1 when orders.Count > 0 => Notification.Create(
                    "Pedido de compra atualizado",
                    "Um pedido de compra teve seu status alterado.",
                    Permissions.Inventory.OrdersManage,
                    orders[i % orders.Count].Value.ToString(),
                    "Inventory",
                    "OrderUpdated"),

                _ when maintenanceRequests.Count > 0 => Notification.Create(
                    "Solicitação de manutenção",
                    "Uma nova solicitação de manutenção foi registrada.",
                    Permissions.Assets.MaintenanceManage,
                    maintenanceRequests[i % maintenanceRequests.Count].Value.ToString(),
                    "Assets",
                    "MaintenanceRequested"),

                _ => Result<Notification>.BusinessRule("no seed data available for this slot")
            };

            if (result.IsFailure)
                continue;

            db.Notifications.Add(result.Data!);
            notifications.Add(result.Data!);
        }

        StampCreationAudit(db);
        await db.SaveChangesAsync();
    }
}
