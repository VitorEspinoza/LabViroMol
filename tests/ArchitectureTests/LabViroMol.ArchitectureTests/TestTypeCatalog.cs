using System.Reflection;
using Microsoft.EntityFrameworkCore;

namespace LabViroMol.ArchitectureTests;

internal static class TestTypeCatalog
{
    public static readonly Assembly[] ProductionAssemblies =
    [
        typeof(LabViroMol.Modules.AdminBff.Presentation.AdminBffModule).Assembly,
        typeof(LabViroMol.Modules.AdminBff.Application.ApplicationModule).Assembly,
        typeof(LabViroMol.Modules.AdminBff.Infrastructure.InfrastructureModule).Assembly,
        typeof(LabViroMol.Modules.Assets.Presentation.AssetsModule).Assembly,
        typeof(LabViroMol.Modules.Assets.Application.ApplicationModule).Assembly,
        typeof(LabViroMol.Modules.Assets.Domain.Equipments.Equipment).Assembly,
        typeof(LabViroMol.Modules.Assets.Infrastructure.InfrastructureModule).Assembly,
        typeof(LabViroMol.Modules.Identity.Presentation.IdentityModule).Assembly,
        typeof(LabViroMol.Modules.Identity.Application.ApplicationModule).Assembly,
        typeof(LabViroMol.Modules.Identity.Contracts.UserInfo).Assembly,
        typeof(LabViroMol.Modules.Identity.Domain.Users.User).Assembly,
        typeof(LabViroMol.Modules.Identity.Infrastructure.InfrastructureModule).Assembly,
        typeof(LabViroMol.Modules.Inventory.Presentation.InventoryModule).Assembly,
        typeof(LabViroMol.Modules.Inventory.Application.ApplicationModule).Assembly,
        typeof(LabViroMol.Modules.Inventory.Domain.Materials.Material).Assembly,
        typeof(LabViroMol.Modules.Inventory.Infrastructure.InfrastructureModule).Assembly,
        typeof(LabViroMol.Modules.Notify.Presentation.NotifyModule).Assembly,
        typeof(LabViroMol.Modules.Notify.Application.ApplicationModule).Assembly,
        typeof(LabViroMol.Modules.Notify.Contracts.ISendEmail).Assembly,
        typeof(LabViroMol.Modules.Notify.Domain.Notifications.Notification).Assembly,
        typeof(LabViroMol.Modules.Notify.Infrastructure.InfrastructureModule).Assembly,
        typeof(LabViroMol.Modules.Research.Presentation.ResearchModule).Assembly,
        typeof(LabViroMol.Modules.Research.Application.ApplicationModule).Assembly,
        typeof(LabViroMol.Modules.Research.Contracts.IProjectCatalog).Assembly,
        typeof(LabViroMol.Modules.Research.Domain.Projects.Project).Assembly,
        typeof(LabViroMol.Modules.Research.Infrastructure.InfrastructureModule).Assembly,
        typeof(LabViroMol.Modules.Scheduling.Presentation.SchedulingModule).Assembly,
        typeof(LabViroMol.Modules.Scheduling.Application.ApplicationModule).Assembly,
        typeof(LabViroMol.Modules.Scheduling.Contracts.ApproveSchedulePersistentEvent).Assembly,
        typeof(LabViroMol.Modules.Scheduling.Domain.Schedules.Schedule).Assembly,
        typeof(LabViroMol.Modules.Scheduling.Infrastructure.InfrastructureModule).Assembly,
        typeof(LabViroMol.Modules.Shared.Infrastructure.SharedModule).Assembly,
        typeof(LabViroMol.Modules.Shared.Kernel.Primitives.AggregateRoot<>).Assembly,
    ];

    public static IEnumerable<Type> AllTypes() =>
        ProductionAssemblies
            .Distinct()
            .SelectMany(assembly => assembly.GetTypes())
            .Where(type => type.FullName is not null);

    public static IEnumerable<Type> ModuleTypes(string moduleName) =>
        AllTypes().Where(type => type.FullName!.Contains($"LabViroMol.Modules.{moduleName}.", StringComparison.Ordinal));

    public static IEnumerable<Type> LayerTypes(string layerName) =>
        AllTypes().Where(type => type.FullName!.Contains($".{layerName}.", StringComparison.Ordinal));

    public static IEnumerable<Type> ContractsTypes() =>
        AllTypes().Where(type => type.FullName!.Contains(".Contracts", StringComparison.Ordinal));

    public static IEnumerable<string> ModulesWithContracts() =>
        ArchitectureModel.IsolatedBusinessModules
            .Where(module => ModuleTypes(module).Any(type => type.FullName!.Contains(".Contracts", StringComparison.Ordinal)));

    public static IEnumerable<Type> CustomExceptions() =>
        AllTypes().Where(type =>
            typeof(Exception).IsAssignableFrom(type) &&
            type.Namespace?.StartsWith("LabViroMol.Modules.", StringComparison.Ordinal) == true &&
            !type.IsAbstract);

    public static IEnumerable<Type> EntityFrameworkConfigurations() =>
        AllTypes().Where(type =>
            type.GetInterfaces().Any(i =>
                i.IsGenericType &&
                i.GetGenericTypeDefinition() == typeof(IEntityTypeConfiguration<>)));
}
