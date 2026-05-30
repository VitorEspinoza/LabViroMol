using LabViroMol.Modules.Identity.Application.Users;
using LabViroMol.Modules.Shared.Infrastructure.Persistence;
using LabViroMol.Modules.Shared.Kernel.Interfaces;
using Mediator;

namespace LabViroMol.Modules.Identity.Infrastructure.Persistence;

public sealed class IdentityUnitOfWork(LabViroMolIdentityDbContext context, IMediator mediator, ICurrentUser currentUser)
    : BaseUnitOfWork<LabViroMolIdentityDbContext>(context, mediator, currentUser), IIdentityUnitOfWork;
