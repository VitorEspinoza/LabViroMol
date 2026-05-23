using Kernel.Persistence;
using LabViroMol.Modules.Assets.Application.Shared;
using Mediator;

namespace LabViroMol.Modules.Assets.Infrastructure.Persistence;

public sealed class AssetsUnitOfWork(AssetsDbContext context, IMediator mediator)
    : BaseUnitOfWork<AssetsDbContext>(context, mediator), IAssetsUnitOfWork;