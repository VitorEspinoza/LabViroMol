using LabViroMol.Modules.Identity.Contracts;
using LabViroMol.Modules.Research.Application.Shared;
using LabViroMol.Modules.Research.Domain.Positions;
using LabViroMol.Modules.Research.Domain.Researchers;
using LabViroMol.Modules.Shared.Kernel.Primitives;
using Mediator;

namespace LabViroMol.Modules.Research.Application.Researchers.Handlers;

public class UserRegisteredIntegrationEventHandler(
    IResearcherRepository researcherRepository,
    IPositionRepository positionRepository,
    IResearchUnitOfWork unitOfWork)
    : INotificationHandler<UserRegisteredIntegrationEvent>
{
    public async ValueTask Handle(UserRegisteredIntegrationEvent notification, CancellationToken ct)
    {
        if (notification.ResearchData is null)
            return;

        var researcherId = ResearcherId.From(notification.UserId.Value);
        var existing = await researcherRepository.GetByIdAsync(researcherId, ct);

        if (existing is not null)
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
