using LabViroMol.Modules.Inventory.Application.Shared;
using LabViroMol.Modules.Shared.Infrastructure.Persistence;
using LabViroMol.Modules.Shared.Infrastructure.Persistence.Outbox;
using LabViroMol.Modules.Shared.Kernel.Interfaces;
using Mediator;

namespace LabViroMol.Modules.Inventory.Infrastructure.Persistence;

public sealed class InventoryUnitOfWork(
    InventoryDbContext context,
    IMediator mediator,
    ICurrentUser currentUser,
    IPersistentEventTypeRegistry eventTypeRegistry)
    : BaseUnitOfWork<InventoryDbContext>(context, mediator, currentUser, eventTypeRegistry), IInventoryUnitOfWork;
