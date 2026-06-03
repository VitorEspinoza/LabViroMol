using System.Threading;
using System.Threading.Tasks;

namespace LabViroMol.Modules.Research.Application.Partners.Commands.Create;

using LabViroMol.Modules.Research.Application.Shared;
using LabViroMol.Modules.Research.Domain.Partners;
using LabViroMol.Modules.Shared.Kernel.Primitives;
using Mediator;

public class CreatePartnerHandler(
    IPartnerRepository partnerRepository,
    IResearchUnitOfWork unitOfWork)
    : ICommandHandler<CreatePartnerCommand, Result>
{
    public async ValueTask<Result> Handle(CreatePartnerCommand command, CancellationToken ct)
    {
        var result = Partner.Create(command.Name, command.Description);
        if (result.IsFailure)
            return result;

        await partnerRepository.AddAsync(result.Data!, ct);
        await unitOfWork.CompleteAsync(ct);

        return Result.Success();
    }
}
