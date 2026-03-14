using Kernel.Persistence;
using LabViroMol.Modules.Inventory.Application.Shared;
using Mediator;

namespace LabViroMol.Modules.Inventory.Infrastructure.Persistence;

public sealed class InventoryUnitOfWork(InventoryDbContext context, IMediator mediator)
    : BaseUnitOfWork<InventoryDbContext>(context, mediator), IInventoryUnitOfWork;