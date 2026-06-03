using System.Threading;
using System.Threading.Tasks;
using LabViroMol.Modules.Identity.Application.Users.Abstractions;
using LabViroMol.Modules.Identity.Contracts;
using LabViroMol.Modules.Identity.Domain.Users;
using LabViroMol.Modules.Shared.Kernel.Identity;
using LabViroMol.Modules.Shared.Kernel.Primitives;
using Mediator;

namespace LabViroMol.Modules.Identity.Application.Users.ReactivateUser;

public class ReactivateUserCommandHandler : ICommandHandler<ReactivateUserCommand, Result>
{
    private readonly IUserRepository _userRepository;
    private readonly IIdentityService _identityService;
    private readonly IIdentityUnitOfWork _unitOfWork;

    public ReactivateUserCommandHandler(
        IUserRepository userRepository,
        IIdentityService identityService,
        IIdentityUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _identityService = identityService;
        _unitOfWork = unitOfWork;
    }

    public async ValueTask<Result> Handle(ReactivateUserCommand command, CancellationToken ct)
    {
        var userId = UserId.From(command.TargetUserId);
        var user = await _userRepository.GetByIdAsync(userId, ct);

        if (user is null)
            return Result.NotFound("Usuário não encontrado.");

        user.Reactivate();

        var lockoutResult = await _identityService.SetUserLockoutAsync(command.TargetUserId, false, ct);
        if (lockoutResult.IsFailure)
            return lockoutResult;

        _unitOfWork.AddIntegrationEvent(new UserReactivatedIntegrationEvent(userId));

        await _unitOfWork.CompleteAsync(ct);

        return Result.Success();
    }
}
