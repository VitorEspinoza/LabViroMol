using LabViroMol.Modules.Inventory.Domain.Materials;
using LabViroMol.Modules.Shared.Kernel.Primitives;
using Mediator;

namespace LabViroMol.Modules.Inventory.Application.Materials.Commands.RemoveStockException;

public record RemoveStockMaterialExceptionCommand(MaterialId MaterialId, Quantity Quantity, string? Reason) : ICommand<Result>;
