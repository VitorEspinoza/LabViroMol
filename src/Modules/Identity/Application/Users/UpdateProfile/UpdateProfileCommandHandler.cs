using System.Threading;
using System.Threading.Tasks;
using LabViroMol.Modules.Identity.Application.Users.Abstractions;
using LabViroMol.Modules.Identity.Contracts;
using LabViroMol.Modules.Identity.Domain.Users;
using LabViroMol.Modules.Shared.Kernel.Identity;
using LabViroMol.Modules.Shared.Kernel.Primitives;
using Mediator;

namespace LabViroMol.Modules.Identity.Application.Users.UpdateProfile;

public class UpdateProfileCommandHandler : ICommandHandler<UpdateProfileCommand, Result>
{
    private readonly IUserRepository _userRepository;
    private readonly IIdentityService _identityService;
    private readonly IIdentityUnitOfWork _unitOfWork;

    public UpdateProfileCommandHandler(
        IUserRepository userRepository,
        IIdentityService identityService,
        IIdentityUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _identityService = identityService;
        _unitOfWork = unitOfWork;
    }

    public async ValueTask<Result> Handle(UpdateProfileCommand command, CancellationToken ct)
    {
        var userId = UserId.From(command.UserId);
        var user = await _userRepository.GetByIdAsync(userId, ct);

        if (user is null)
            return Result.NotFound("Usuário não encontrado.");

        var data = command.UserData;
        var name = new UserName(data.FirstName, data.LastName);

        user.Update(name, user.Email, data.PhoneNumber, data.EmergencyContactNumber);

        var roleIds = await _identityService.GetUserRoleIdsAsync(command.UserId, ct);

        _unitOfWork.AddIntegrationEvent(new UserUpdatedIntegrationEvent(
            userId,
            data.FirstName,
            data.LastName,
            data.PhoneNumber,
            data.EmergencyContactNumber,
            roleIds,
            data.ResearchData));

        await _unitOfWork.CompleteAsync(ct);

        return Result.Success();
    }
}
