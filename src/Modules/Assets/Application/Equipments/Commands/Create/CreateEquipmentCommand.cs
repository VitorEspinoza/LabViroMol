using LabViroMol.Modules.Shared.Abstractions.Primitives;
using Mediator;

namespace LabViroMol.Modules.Assets.Application.Equipments.Commands.Create;

public record CreateEquipmentCommand(string Name, string Model, string Brand, string Code, string Description) : ICommand<Result>;