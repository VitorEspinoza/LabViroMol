using Kernel.Persistence;
using LabViroMol.Modules.Scheduling.Application.Shared;
using Mediator;

namespace LabViroMol.Modules.Scheduling.Infrastructure.Persistence;

public sealed class SchedulingUnitOfWork(SchedulingDbContext context, IMediator mediator)
    : BaseUnitOfWork<SchedulingDbContext>(context, mediator), ISchedulingUnitOfWork;