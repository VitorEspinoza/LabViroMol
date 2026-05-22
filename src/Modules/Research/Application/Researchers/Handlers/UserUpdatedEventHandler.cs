using LabViroMol.Modules.Shared.Abstractions.Primitives;
using LabViroMol.Modules.Research.Application.External;
using LabViroMol.Modules.Research.Domain.Positions;
using LabViroMol.Modules.Research.Domain.Researchers;

namespace LabViroMol.Modules.Research.Application.Researchers.Handlers;

using Mediator;

public class UserUpdatedEventHandler(
    IResearcherRepository researcherRepository,
    IPositionRepository positionRepository)
    : INotificationHandler<UserUpdatedEvent>
{
    public async ValueTask Handle(UserUpdatedEvent notification, CancellationToken ct)
    {
        
        var researcher = await researcherRepository.GetByUserIdAsync(notification.TargetUserId, ct);

        var hasResearcherData = notification.Data is { Academic: not null, Research: not null };
        if (researcher is null && hasResearcherData)
        {
            await AddResearcher(notification, ct);
            return;
        }

        if (researcher is not null && !hasResearcherData)
        {
            researcher.MarkAsRemoved(notification.RequestedBy);
            return;
        }

        if (researcher is not null && hasResearcherData)
            await UpdateResearcher(researcher!, notification, ct);

    }

    private async Task AddResearcher(UserUpdatedEvent notification, CancellationToken ct)
    {
        await ValidatePosition(PositionId.From(notification.Data.Research!.PositionId), ct);
     
            
        var background = new AcademicBackground(  
            DegreeLevel.FromString(notification.Data.Academic!.DegreeLevel), 
            notification.Data.Academic.FieldOfStudy
            );

        var researcherName = new ResearcherName(
            notification.Data.Identity.FirstName,
            notification.Data.Identity.LastName,
            notification.Data.Research.CitationName,
            notification.Data.Research.DisplayName
            );
            
        var newResearcher = Researcher.Create(
            ResearcherId.From(notification.TargetUserId.Value),
            notification.RequestedBy, researcherName,
            notification.Data.Research.LattesUrl,
            background, 
            PositionId.From(notification.Data.Research.PositionId)
            );

        await researcherRepository.AddAsync(newResearcher, ct);
    }

    private async Task UpdateResearcher(Researcher researcher, UserUpdatedEvent notification, CancellationToken ct)
    {
        await ValidatePosition(PositionId.From(notification.Data.Research!.PositionId), ct);
        
        researcher!.Update(
            DegreeLevel.FromString(notification.Data.Academic!.DegreeLevel), 
            notification.Data.Academic.FieldOfStudy,
            PositionId.From(notification.Data.Research!.PositionId),
            notification.RequestedBy
            );
        
    }

    private async Task ValidatePosition(PositionId positionId, CancellationToken ct)
    {
        var position = await positionRepository.GetByIdAsync(
            PositionId.From(positionId), ct);
        
        if(position is null)
            throw new DomainException($"Cargo inválido.");
    }
}
