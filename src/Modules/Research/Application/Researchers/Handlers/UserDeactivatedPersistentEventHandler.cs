using LabViroMol.Modules.Identity.Contracts;
using LabViroMol.Modules.Research.Application.Shared;
using LabViroMol.Modules.Research.Domain.Researchers;
using Mediator;

namespace LabViroMol.Modules.Research.Application.Researchers.Handlers;

public sealed class UserDeactivatedPersistentEventHandler(
    IResearcherRepository researcherRepository,
    IResearchUnitOfWork unitOfWork)
    : INotificationHandler<UserDeactivatedPersistentEvent>
{
    public async ValueTask Handle(UserDeactivatedPersistentEvent notification, CancellationToken ct)
    {
        var researcherId = ResearcherId.From(notification.UserId.Value);
        var researcher = await researcherRepository.GetByIdAsync(researcherId, ct);

        if (researcher is null)
            return;

        researcher.Deactivate();
        await unitOfWork.CompleteAsync(ct);
    }
}
