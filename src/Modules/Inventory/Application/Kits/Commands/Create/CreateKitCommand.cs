using System.Collections.Generic;
using LabViroMol.Modules.Inventory.Application.Kits.Commands.Shared;
using LabViroMol.Modules.Shared.Kernel.Primitives;
using Mediator;

namespace LabViroMol.Modules.Inventory.Application.Kits.Commands.Create;

public record CreateKitCommand(string Name, string Description, List<KitItemInputModel> Materials) : ICommand<Result>;
