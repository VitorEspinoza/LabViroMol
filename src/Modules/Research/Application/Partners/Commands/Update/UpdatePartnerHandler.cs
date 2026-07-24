namespace LabViroMol.Modules.Research.Application.Partners.Commands.Update;

using LabViroMol.Modules.Research.Application.Shared;
using LabViroMol.Modules.Research.Domain.Partners;
using LabViroMol.Modules.Shared.Kernel.Primitives;
using Mediator;

public sealed class UpdatePartnerHandler(
    IPartnerRepository partnerRepository,
    IResearchUnitOfWork unitOfWork)
    : ICommandHandler<UpdatePartnerCommand, Result>
{
    public async ValueTask<Result> Handle(UpdatePartnerCommand command, CancellationToken ct)
    {
        var partner = await partnerRepository.GetByIdAsync(PartnerId.From(command.PartnerId), ct);
        if (partner is null)
            return Result.NotFound("Parceiro nao encontrado.");

        var result = partner.Update(command.Name, command.Description);
        if (result.IsFailure)
            return result;

        await unitOfWork.CompleteAsync(ct);
        return Result.Success();
    }
}
