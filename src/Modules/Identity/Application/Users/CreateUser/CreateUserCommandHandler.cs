using LabViroMol.Modules.Identity.Application.Users.Abstractions;
using LabViroMol.Modules.Identity.Contracts;
using LabViroMol.Modules.Identity.Domain.Users;
using LabViroMol.Modules.Shared.Kernel.Identity;
using LabViroMol.Modules.Shared.Kernel.Primitives;
using Mediator;

namespace LabViroMol.Modules.Identity.Application.Users.CreateUser;

public class CreateUserCommandHandler : ICommandHandler<CreateUserCommand, Result<string>>
{
    private readonly IIdentityService _identityService;
    private readonly IUserRepository _userRepository;
    private readonly IIdentityUnitOfWork _unitOfWork;

    public CreateUserCommandHandler(
        IIdentityService identityService,
        IUserRepository userRepository,
        IIdentityUnitOfWork unitOfWork)
    {
        _identityService = identityService;
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
    }

    public async ValueTask<Result<string>> Handle(CreateUserCommand command, CancellationToken ct)
    {
        var identityResult = await _identityService.CreateUserAsync(command.Email, ct);

        if (identityResult.IsFailure)
            return Result<string>.Validation(identityResult.Errors);

        var (applicationUserId, resetToken) = identityResult.Data!;

        var userId = UserId.From(applicationUserId);
        var data = command.UserData;
        var name = new UserName(data.FirstName, data.LastName);
        var email = new Email(command.Email);

        var user = User.Create(
            userId,
            name,
            email,
            data.PhoneNumber,
            data.EmergencyContactNumber);

        await _userRepository.AddAsync(user, ct);

        if (command.RoleIds.Any())
        {
            var rolesResult = await _identityService.UpdateUserRolesAsync(applicationUserId, command.RoleIds, ct);
            if (rolesResult.IsFailure)
                return Result<string>.Validation(rolesResult.Errors);
        }

        _unitOfWork.AddIntegrationEvent(new UserRegisteredIntegrationEvent(
            userId,
            command.Email,
            data.FirstName,
            data.LastName,
            command.RoleIds,
            data.ResearchData));

        await _unitOfWork.CompleteAsync(ct);

        return Result<string>.Success(resetToken);
    }
}
