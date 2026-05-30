using LabViroMol.Modules.Shared.Infrastructure.Persistence;
using LabViroMol.Modules.Shared.Kernel.Interfaces;
using LabViroMol.Modules.Inventory.Application.Shared;
using Mediator;

namespace LabViroMol.Modules.Inventory.Infrastructure.Persistence;

public sealed class InventoryUnitOfWork(InventoryDbContext context, IMediator mediator, ICurrentUser currentUser)
    : BaseUnitOfWork<InventoryDbContext>(context, mediator, currentUser), IInventoryUnitOfWork;