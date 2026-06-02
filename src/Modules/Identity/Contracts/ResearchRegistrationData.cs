namespace LabViroMol.Modules.Identity.Contracts;

public record ResearchRegistrationData(
    Guid PositionId,
    string DegreeLevel,
    string FieldOfStudy,
    string? LattesUrl,
    string? CitationName,
    string? DisplayName);

