using LabViroMol.Modules.Identity.Contracts;
using LabViroMol.Modules.Research.Application.Shared;
using LabViroMol.Modules.Research.Domain.Researchers;
using Mediator;

namespace LabViroMol.Modules.Research.Application.Researchers.Handlers;

public class UserDeactivatedIntegrationEventHandler(
    IResearcherRepository researcherRepository,
    IResearchUnitOfWork unitOfWork)
    : INotificationHandler<UserDeactivatedIntegrationEvent>
{
    public async ValueTask Handle(UserDeactivatedIntegrationEvent notification, CancellationToken ct)
    {
        var researcherId = ResearcherId.From(notification.UserId.Value);
        var researcher = await researcherRepository.GetByIdAsync(researcherId, ct);

        if (researcher is null)
            return;

        researcher.Deactivate();
        await unitOfWork.CompleteAsync(ct);
    }
}
