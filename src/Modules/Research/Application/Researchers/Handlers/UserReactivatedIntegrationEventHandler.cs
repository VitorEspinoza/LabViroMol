using LabViroMol.Modules.Identity.Contracts;
using LabViroMol.Modules.Research.Application.Shared;
using LabViroMol.Modules.Research.Domain.Researchers;
using Mediator;

namespace LabViroMol.Modules.Research.Application.Researchers.Handlers;

public class UserReactivatedIntegrationEventHandler(
    IResearcherRepository researcherRepository,
    IResearchUnitOfWork unitOfWork)
    : INotificationHandler<UserReactivatedIntegrationEvent>
{
    public async ValueTask Handle(UserReactivatedIntegrationEvent notification, CancellationToken ct)
    {
        var researcherId = ResearcherId.From(notification.UserId.Value);
        var researcher = await researcherRepository.GetByIdAsync(researcherId, ct);

        if (researcher is null)
            return;

        researcher.Reactivate();
        await unitOfWork.CompleteAsync(ct);
    }
}
