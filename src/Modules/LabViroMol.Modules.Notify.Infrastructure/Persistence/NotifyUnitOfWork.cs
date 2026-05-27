using Kernel.Persistence;
using LabViroMol.Modules.Notify.Application.Shared;
using Mediator;

namespace LabViroMol.Modules.Notify.Infrastructure.Persistence;

public sealed class NotifyUnitOfWork(NotifyDbContext context, IMediator mediator)
    : BaseUnitOfWork<NotifyDbContext>(context, mediator), INotifyUnitOfWork;