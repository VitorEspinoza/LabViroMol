namespace LabViroMol.Modules.Research.Infrastructure.Persistence;

using Kernel.Persistence;
using LabViroMol.Modules.Research.Application.Shared;
using Mediator;

public sealed class ResearchUnitOfWork(ResearchDbContext context, IMediator mediator)
    : BaseUnitOfWork<ResearchDbContext>(context, mediator), IResearchUnitOfWork;
