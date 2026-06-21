using LabViroMol.Modules.Identity.Application.Users.Abstractions;
using LabViroMol.Modules.Identity.Application.Users.Emails;
using LabViroMol.Modules.Identity.Contracts;
using LabViroMol.Modules.Identity.Domain.Users;
using LabViroMol.Modules.Shared.Kernel.Identity;
using LabViroMol.Modules.Shared.Kernel.Primitives;
using Mediator;

namespace LabViroMol.Modules.Identity.Application.Users.CreateUser;

public class CreateUserCommandHandler : ICommandHandler<CreateUserCommand, Result>
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

    public async ValueTask<Result> Handle(CreateUserCommand command, CancellationToken ct)
    {
        var identityResult = await _identityService.CreateUserAsync(command.Email, ct);

        if (identityResult.IsFailure)
            return Result.Validation(identityResult.Errors);

        var (applicationUserId, resetLink) = identityResult.Data!;

        var userId = UserId.From(applicationUserId);
        var data = command.UserData;
        var name = new UserName(data.FirstName, data.LastName);
        var email = new Email(command.Email);

        var emergencyContact = EmergencyContact.FromNullable(data.EmergencyContactName, data.EmergencyContactNumber);

        var user = User.Create(
            userId,
            name,
            email,
            data.PhoneNumber,
            emergencyContact);

        await _userRepository.AddAsync(user, ct);

        if (command.RoleIds.Any())
        {
            var rolesResult = await _identityService.UpdateUserRolesAsync(applicationUserId, command.RoleIds, ct);
            if (rolesResult.IsFailure)
                return Result.Validation(rolesResult.Errors);
        }

        _unitOfWork.AddPersistentEvent(new UserRegisteredPersistentEvent(
            userId,
            command.Email,
            data.FirstName,
            data.LastName,
            command.RoleIds,
            data.ResearchData));
        
        var (subject, body) = PasswordEmailTemplates.BuildWelcomeEmail(data.FirstName, resetLink);
        
        _unitOfWork.AddPersistentEvent(new ResetPasswordPersistentEvent(
            command.Email,
            subject,
            body));

        await _unitOfWork.CompleteAsync(ct);

        return Result.Success();
    }
}
