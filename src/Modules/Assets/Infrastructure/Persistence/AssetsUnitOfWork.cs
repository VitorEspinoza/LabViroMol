using LabViroMol.Modules.Assets.Application.Shared;
using LabViroMol.Modules.Shared.Infrastructure.Persistence;
using LabViroMol.Modules.Shared.Kernel.Interfaces;
using Mediator;

namespace LabViroMol.Modules.Assets.Infrastructure.Persistence;

public sealed class AssetsUnitOfWork(AssetsDbContext context, IMediator mediator, ICurrentUser currentUser)
    : BaseUnitOfWork<AssetsDbContext>(context, mediator, currentUser), IAssetsUnitOfWork;