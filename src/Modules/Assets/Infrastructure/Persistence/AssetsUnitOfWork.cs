using LabViroMol.Modules.Assets.Application.Shared;
using LabViroMol.Modules.Shared.Infrastructure.Persistence;
using LabViroMol.Modules.Shared.Infrastructure.Persistence.Outbox;
using LabViroMol.Modules.Shared.Kernel.Interfaces;
using Mediator;

namespace LabViroMol.Modules.Assets.Infrastructure.Persistence;

public sealed class AssetsUnitOfWork(
    AssetsDbContext context,
    IMediator mediator,
    ICurrentUser currentUser,
    IPersistentEventTypeRegistry eventTypeRegistry)
    : BaseUnitOfWork<AssetsDbContext>(context, mediator, currentUser, eventTypeRegistry), IAssetsUnitOfWork;
