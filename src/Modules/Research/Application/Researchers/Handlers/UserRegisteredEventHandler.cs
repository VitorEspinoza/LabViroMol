using LabViroMol.Modules.Research.Application.External;
using LabViroMol.Modules.Research.Domain.Positions;
using LabViroMol.Modules.Research.Domain.Researchers;
using LabViroMol.Modules.Shared.Kernel.Primitives;
using Mediator;

namespace LabViroMol.Modules.Research.Application.Researchers.Handlers;

public class UserRegisteredEventHandler(
    IResearcherRepository researcherRepository,
    IPositionRepository positionRepository)
    : INotificationHandler<UserRegisteredEvent>
{
    public async ValueTask Handle(UserRegisteredEvent notification, CancellationToken ct)
    {

        if (notification.Data.Research is null || notification.Data.Academic is null)
            return;

        var position = await positionRepository.GetByIdAsync(PositionId.From(notification.Data.Research.PositionId), ct);

        if (position is null)
            throw new DomainException("Cargo inválido selecionado para usuário");

        var background = new AcademicBackground(
            DegreeLevel.FromString(notification.Data.Academic!.DegreeLevel),
            notification.Data.Academic.FieldOfStudy
            );

        var name = new ResearcherName(notification.Data.Identity.FirstName, notification.Data.Identity.LastName, notification.Data.Research.CitationName, notification.Data.Research.DisplayName);

        var researcher = Researcher.Create(
            ResearcherId.From(notification.TargetUserId.Value),
            name,
            notification.Data.Research.LattesUrl,
            background,
            position.Id);

        await researcherRepository.AddAsync(researcher, ct);
    }
}
