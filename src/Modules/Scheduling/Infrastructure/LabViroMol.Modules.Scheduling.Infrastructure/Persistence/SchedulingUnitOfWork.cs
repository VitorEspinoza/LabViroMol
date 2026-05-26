using LabViroMol.Modules.Shared.Infrastructure.Persistence;
using LabViroMol.Modules.Shared.Kernel.Interfaces;
using LabViroMol.Modules.Scheduling.Application.Shared;
using Mediator;

namespace LabViroMol.Modules.Scheduling.Infrastructure.Persistence;

public sealed class SchedulingUnitOfWork(SchedulingDbContext context, IMediator mediator, ICurrentUser currentUser)
    : BaseUnitOfWork<SchedulingDbContext>(context, mediator, currentUser), ISchedulingUnitOfWork;