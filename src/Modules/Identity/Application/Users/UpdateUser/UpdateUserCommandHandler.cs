using LabViroMol.Modules.Identity.Application.Users.Abstractions;
using LabViroMol.Modules.Identity.Contracts;
using LabViroMol.Modules.Identity.Domain.Users;
using LabViroMol.Modules.Shared.Kernel.Identity;
using LabViroMol.Modules.Shared.Kernel.Primitives;
using Mediator;

namespace LabViroMol.Modules.Identity.Application.Users.UpdateUser;

public sealed class UpdateUserCommandHandler : ICommandHandler<UpdateUserCommand, Result>
{
    private readonly IUserRepository _userRepository;
    private readonly IIdentityService _identityService;
    private readonly IIdentityUnitOfWork _unitOfWork;

    public UpdateUserCommandHandler(
        IUserRepository userRepository,
        IIdentityService identityService,
        IIdentityUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _identityService = identityService;
        _unitOfWork = unitOfWork;
    }

    public async ValueTask<Result> Handle(UpdateUserCommand command, CancellationToken ct)
    {
        var userId = UserId.From(command.TargetUserId);
        var user = await _userRepository.GetByIdAsync(userId, ct);

        if (user is null)
            return Result.NotFound("Usuário não encontrado.");

        var data = command.UserData;
        var name = new UserName(data.FirstName, data.LastName);
        var emergencyContact = EmergencyContact.FromNullable(data.EmergencyContactName, data.EmergencyContactNumber);

        user.Update(name, user.Email, data.PhoneNumber, emergencyContact);

        var rolesResult = await _identityService.UpdateUserRolesAsync(command.TargetUserId, command.RoleIds, ct);
        if (rolesResult.IsFailure)
            return rolesResult;

        _unitOfWork.AddPersistentEvent(new UserUpdatedPersistentEvent(
            userId,
            data.FirstName,
            data.LastName,
            data.PhoneNumber,
            data.EmergencyContactName,
            data.EmergencyContactNumber,
            command.RoleIds,
            data.ResearchData));

        await _unitOfWork.CompleteAsync(ct);

        return Result.Success();
    }
}
