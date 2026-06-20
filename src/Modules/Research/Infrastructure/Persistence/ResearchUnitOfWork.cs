using LabViroMol.Modules.Research.Application.Shared;
using LabViroMol.Modules.Shared.Infrastructure.Persistence;
using LabViroMol.Modules.Shared.Infrastructure.Persistence.Outbox;
using LabViroMol.Modules.Shared.Kernel.Interfaces;
using Mediator;

namespace LabViroMol.Modules.Research.Infrastructure.Persistence;

public sealed class ResearchUnitOfWork(
    ResearchDbContext context,
    IMediator mediator,
    ICurrentUser currentUser,
    IPersistentEventTypeRegistry eventTypeRegistry)
    : BaseUnitOfWork<ResearchDbContext>(context, mediator, currentUser, eventTypeRegistry), IResearchUnitOfWork;
