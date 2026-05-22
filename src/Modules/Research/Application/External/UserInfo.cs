namespace LabViroMol.Modules.Research.Application.External;


public record UserIdentityData(string FirstName, string LastName, string Email);
public record UserResearchData(string? LattesUrl, string? CitationName, string? DisplayName, Guid PositionId);
public record UserAcademicData(string DegreeLevel, string FieldOfStudy);

public record UserProfilePayload(
    UserIdentityData Identity, 
    UserResearchData? Research, 
    UserAcademicData? Academic);

