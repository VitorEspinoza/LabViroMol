using LabViroMol.Modules.Notify.Application.Shared;
using LabViroMol.Modules.Shared.Infrastructure.Persistence;
using LabViroMol.Modules.Shared.Infrastructure.Persistence.Outbox;
using LabViroMol.Modules.Shared.Kernel.Interfaces;
using Mediator;

namespace LabViroMol.Modules.Notify.Infrastructure.Persistence;

public sealed class NotifyUnitOfWork(
    NotifyDbContext context,
    IMediator mediator,
    ICurrentUser currentUser,
    IPersistentEventTypeRegistry eventTypeRegistry)
    : BaseUnitOfWork<NotifyDbContext>(context, mediator, currentUser, eventTypeRegistry), INotifyUnitOfWork;
