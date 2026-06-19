using LabViroMol.Modules.Scheduling.Application.Shared;
using LabViroMol.Modules.Shared.Infrastructure.Persistence;
using LabViroMol.Modules.Shared.Infrastructure.Persistence.Outbox;
using LabViroMol.Modules.Shared.Kernel.Interfaces;
using Mediator;

namespace LabViroMol.Modules.Scheduling.Infrastructure.Persistence;

public sealed class SchedulingUnitOfWork(
    SchedulingDbContext context,
    IMediator mediator,
    ICurrentUser currentUser,
    IPersistentEventTypeRegistry eventTypeRegistry)
    : BaseUnitOfWork<SchedulingDbContext>(context, mediator, currentUser, eventTypeRegistry), ISchedulingUnitOfWork;
