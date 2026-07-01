using ArchUnitNET.Domain;
using ArchUnitNET.Domain.Extensions;
using ArchUnitNET.Fluent;
using ArchUnitNET.Loader;
using LabViroMol.Modules.AdminBff.Application;
using LabViroMol.Modules.AdminBff.Infrastructure;
using LabViroMol.Modules.AdminBff.Presentation;
using LabViroMol.Modules.Assets.Application;
using LabViroMol.Modules.Assets.Domain;
using LabViroMol.Modules.Assets.Infrastructure;
using LabViroMol.Modules.Assets.Presentation;
using LabViroMol.Modules.Identity.Application;
using LabViroMol.Modules.Identity.Contracts;
using LabViroMol.Modules.Identity.Domain;
using LabViroMol.Modules.Identity.Infrastructure;
using LabViroMol.Modules.Identity.Presentation;
using LabViroMol.Modules.Inventory.Application;
using LabViroMol.Modules.Inventory.Domain;
using LabViroMol.Modules.Inventory.Infrastructure;
using LabViroMol.Modules.Inventory.Presentation;
using LabViroMol.Modules.Notify.Application;
using LabViroMol.Modules.Notify.Contracts;
using LabViroMol.Modules.Notify.Domain;
using LabViroMol.Modules.Notify.Infrastructure;
using LabViroMol.Modules.Notify.Presentation;
using LabViroMol.Modules.Research.Application;
using LabViroMol.Modules.Research.Contracts;
using LabViroMol.Modules.Research.Domain;
using LabViroMol.Modules.Research.Infrastructure;
using LabViroMol.Modules.Research.Presentation;
using LabViroMol.Modules.Scheduling.Application;
using LabViroMol.Modules.Scheduling.Domain;
using LabViroMol.Modules.Scheduling.Infrastructure;
using LabViroMol.Modules.Scheduling.Presentation;
using LabViroMol.Modules.Shared.Infrastructure;
using LabViroMol.Modules.Shared.Kernel;
using static ArchUnitNET.Fluent.ArchRuleDefinition;

namespace LabViroMol.ArchitectureTests;

public static class ArchitectureModel
{
    public static readonly Architecture Architecture = new ArchLoader()
        .LoadAssemblies(
            typeof(AdminBffModule).Assembly,
            typeof(LabViroMol.Modules.AdminBff.Application.ApplicationModule).Assembly,
            typeof(LabViroMol.Modules.AdminBff.Infrastructure.InfrastructureModule).Assembly,
            typeof(AssetsModule).Assembly,
            typeof(LabViroMol.Modules.Assets.Application.ApplicationModule).Assembly,
            typeof(LabViroMol.Modules.Assets.Domain.Equipments.Equipment).Assembly,
            typeof(LabViroMol.Modules.Assets.Infrastructure.InfrastructureModule).Assembly,
            typeof(IdentityModule).Assembly,
            typeof(LabViroMol.Modules.Identity.Application.ApplicationModule).Assembly,
            typeof(LabViroMol.Modules.Identity.Contracts.UserInfo).Assembly,
            typeof(LabViroMol.Modules.Identity.Domain.Users.User).Assembly,
            typeof(LabViroMol.Modules.Identity.Infrastructure.InfrastructureModule).Assembly,
            typeof(InventoryModule).Assembly,
            typeof(LabViroMol.Modules.Inventory.Application.ApplicationModule).Assembly,
            typeof(LabViroMol.Modules.Inventory.Domain.Materials.Material).Assembly,
            typeof(LabViroMol.Modules.Inventory.Infrastructure.InfrastructureModule).Assembly,
            typeof(NotifyModule).Assembly,
            typeof(LabViroMol.Modules.Notify.Application.ApplicationModule).Assembly,
            typeof(LabViroMol.Modules.Notify.Contracts.ISendEmail).Assembly,
            typeof(LabViroMol.Modules.Notify.Domain.Notifications.Notification).Assembly,
            typeof(LabViroMol.Modules.Notify.Infrastructure.InfrastructureModule).Assembly,
            typeof(ResearchModule).Assembly,
            typeof(LabViroMol.Modules.Research.Application.ApplicationModule).Assembly,
            typeof(LabViroMol.Modules.Research.Contracts.IProjectCatalog).Assembly,
            typeof(LabViroMol.Modules.Research.Domain.Projects.Project).Assembly,
            typeof(LabViroMol.Modules.Research.Infrastructure.InfrastructureModule).Assembly,
            typeof(SchedulingModule).Assembly,
            typeof(LabViroMol.Modules.Scheduling.Application.ApplicationModule).Assembly,
            typeof(LabViroMol.Modules.Scheduling.Contracts.ApproveSchedulePersistentEvent).Assembly,
            typeof(LabViroMol.Modules.Scheduling.Domain.Schedules.Schedule).Assembly,
            typeof(LabViroMol.Modules.Scheduling.Infrastructure.InfrastructureModule).Assembly,
            typeof(SharedModule).Assembly,
            typeof(LabViroMol.Modules.Shared.Kernel.Primitives.AggregateRoot<>).Assembly)
        .Build();

    public static readonly IObjectProvider<IType> Domain = Types()
        .That()
        .HaveFullNameContaining(".Domain.")
        .As("Domain Layer");

    public static readonly IObjectProvider<IType> Application = Types()
        .That()
        .HaveFullNameContaining(".Application.")
        .As("Application Layer");

    public static readonly IObjectProvider<IType> Infrastructure = Types()
        .That()
        .HaveFullNameContaining(".Infrastructure.")
        .As("Infrastructure Layer");

    public static readonly IObjectProvider<IType> Presentation = Types()
        .That()
        .HaveFullNameContaining(".Presentation.")
        .As("Presentation Layer");

    public static readonly IObjectProvider<IType> Contracts = Types()
        .That()
        .HaveFullNameContaining(".Contracts")
        .As("Contracts Layer");

    public static readonly IObjectProvider<IType> SharedKernel = Types()
        .That()
        .HaveFullNameContaining("LabViroMol.Modules.Shared.Kernel")
        .As("Shared Kernel");

    public static readonly IObjectProvider<IType> SharedInfrastructure = Types()
        .That()
        .HaveFullNameContaining("LabViroMol.Modules.Shared.Infrastructure")
        .As("Shared Infrastructure");

    public static readonly string[] BusinessModules =
    [
        "AdminBff",
        "Assets",
        "Identity",
        "Inventory",
        "Notify",
        "Research",
        "Scheduling",
    ];

    public static readonly string[] IsolatedBusinessModules = BusinessModules
        .Where(module => module != "AdminBff")
        .ToArray();
}
