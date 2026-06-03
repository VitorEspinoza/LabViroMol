using System.Threading;
using System.Threading.Tasks;
using LabViroMol.Modules.Identity.Contracts;
using LabViroMol.Modules.Research.Application.Shared;
using LabViroMol.Modules.Research.Domain.Positions;
using LabViroMol.Modules.Research.Domain.Researchers;
using LabViroMol.Modules.Shared.Kernel.Primitives;
using Mediator;

namespace LabViroMol.Modules.Research.Application.Researchers.Handlers;

public class UserUpdatedIntegrationEventHandler(
    IResearcherRepository researcherRepository,
    IPositionRepository positionRepository,
    IResearchUnitOfWork unitOfWork)
    : INotificationHandler<UserUpdatedIntegrationEvent>
{
    public async ValueTask Handle(UserUpdatedIntegrationEvent notification, CancellationToken ct)
    {
        var researcherId = ResearcherId.From(notification.UserId.Value);
        var existing = await researcherRepository.GetByIdAsync(researcherId, ct);

        if (existing is not null && notification.ResearchData is null)
        {
            researcherRepository.Delete(existing);
            await unitOfWork.CompleteAsync(ct);
            return;
        }

        if (existing is not null)
        {
            var updatedName = new ResearcherName(
                notification.FirstName,
                notification.LastName,
                notification.ResearchData!.CitationName,
                notification.ResearchData.DisplayName);

            existing.UpdateName(updatedName);

            var degreeLevel = DegreeLevel.FromString(notification.ResearchData.DegreeLevel);
            existing.Update(degreeLevel, notification.ResearchData.FieldOfStudy,
                PositionId.From(notification.ResearchData.PositionId));

            await unitOfWork.CompleteAsync(ct);
            return;
        }

        if (notification.ResearchData is null)
            return;

        var data = notification.ResearchData;
        var position = await positionRepository.GetByIdAsync(PositionId.From(data.PositionId), ct);

        if (position is null)
            throw new DomainException("Cargo inválido selecionado para usuário");

        var background = new AcademicBackground(
            DegreeLevel.FromString(data.DegreeLevel),
            data.FieldOfStudy);

        var name = new ResearcherName(
            notification.FirstName,
            notification.LastName,
            data.CitationName,
            data.DisplayName);

        var researcher = Researcher.Create(
            researcherId,
            name,
            data.LattesUrl,
            background,
            position.Id);

        await researcherRepository.AddAsync(researcher, ct);
        await unitOfWork.CompleteAsync(ct);
    }
}
