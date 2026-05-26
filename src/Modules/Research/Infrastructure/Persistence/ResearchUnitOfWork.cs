namespace LabViroMol.Modules.Research.Infrastructure.Persistence;

using LabViroMol.Modules.Shared.Infrastructure.Persistence;
using LabViroMol.Modules.Shared.Kernel.Interfaces;
using LabViroMol.Modules.Research.Application.Shared;
using Mediator;

public sealed class ResearchUnitOfWork(ResearchDbContext context, IMediator mediator, ICurrentUser currentUser)
    : BaseUnitOfWork<ResearchDbContext>(context, mediator, currentUser), IResearchUnitOfWork;
