using LabViroMol.Modules.Shared.Kernel.Primitives;

namespace LabViroMol.Modules.Assets.Domain.MaintenanceRequests;

public record struct MaintenanceRequestId(Guid Value) : IStrongId<MaintenanceRequestId>
{
    public static MaintenanceRequestId From(Guid value) => new(value);
    public override string ToString() => Value.ToString();
    public static implicit operator Guid(MaintenanceRequestId id) => id.Value;
}
