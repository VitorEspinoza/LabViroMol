using LabViroMol.Modules.Notify.Application.Shared;
using LabViroMol.Modules.Shared.Infrastructure.Persistence;
using LabViroMol.Modules.Shared.Kernel.Interfaces;
using Mediator;

namespace LabViroMol.Modules.Notify.Infrastructure.Persistence;

public sealed class NotifyUnitOfWork(NotifyDbContext context, IMediator mediator, ICurrentUser currentUser)
    : BaseUnitOfWork<NotifyDbContext>(context, mediator, currentUser), INotifyUnitOfWork;