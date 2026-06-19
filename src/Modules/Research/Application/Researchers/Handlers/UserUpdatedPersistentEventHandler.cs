using LabViroMol.Modules.Identity.Contracts;
using LabViroMol.Modules.Research.Application.Shared;
using LabViroMol.Modules.Research.Domain.Positions;
using LabViroMol.Modules.Research.Domain.Researchers;
using LabViroMol.Modules.Shared.Kernel.Primitives;
using Mediator;

namespace LabViroMol.Modules.Research.Application.Researchers.Handlers;

public class UserUpdatedPersistentEventHandler(
    IResearcherRepository researcherRepository,
    IPositionRepository positionRepository,
    IResearchUnitOfWork unitOfWork)
    : INotificationHandler<UserUpdatedPersistentEvent>
{
    public async ValueTask Handle(UserUpdatedPersistentEvent notification, CancellationToken ct)
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
            var positionId = notification.ResearchData.PositionId.HasValue
                ? PositionId.From(notification.ResearchData.PositionId.Value)
                : existing.PositionId;

            existing.Update(degreeLevel, notification.ResearchData.FieldOfStudy, positionId);

            await unitOfWork.CompleteAsync(ct);
            return;
        }

        if (notification.ResearchData is null)
            return;

        var data = notification.ResearchData;

        // Position is required to create a researcher profile; skip if not provided (admin must assign it)
        if (!data.PositionId.HasValue)
            return;

        var position = await positionRepository.GetByIdAsync(PositionId.From(data.PositionId.Value), ct);

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
