namespace LabViroMol.Modules.Research.Application.Partners.Commands.Delete;

using LabViroMol.Modules.Research.Application.Shared;
using LabViroMol.Modules.Research.Domain.Partners;
using LabViroMol.Modules.Shared.Kernel.Interfaces;
using LabViroMol.Modules.Shared.Kernel.Primitives;
using Mediator;

public class DeletePartnerHandler(
    IPartnerRepository partnerRepository,
    ICurrentUser currentUser,
    IResearchUnitOfWork unitOfWork)
    : ICommandHandler<DeletePartnerCommand, Result>
{
    public async ValueTask<Result> Handle(DeletePartnerCommand command, CancellationToken ct)
    {
        var partner = await partnerRepository.GetByIdAsync(PartnerId.From(command.PartnerId), ct);
        if (partner is null)
            return Result.NotFound("Parceiro nao encontrado.");

        partner.MarkAsRemoved(currentUser.Id);
        await unitOfWork.CompleteAsync(ct);
        return Result.Success();
    }
}
