using LabViroMol.Modules.Shared.Kernel.Primitives;
using Mediator;

namespace LabViroMol.Modules.Assets.Application.Equipments.Commands.Create;

public record CreateEquipmentCommand(string Name, string Model, string Brand, string Code, string Description, string? Location = null) : ICommand<Result>;
