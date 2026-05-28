using LabViroMol.Modules.Inventory.Application.Kits.Commands.Shared;
using LabViroMol.Modules.Inventory.Domain.Kits;
using LabViroMol.Modules.Shared.Kernel.Primitives;
using Mediator;

namespace LabViroMol.Modules.Inventory.Application.Kits.Commands.Update;

public record UpdateKitCommand(
    KitId KitId,
    string Name,
    string Description,
    List<KitItemInputModel> Materials) : ICommand<Result>;
