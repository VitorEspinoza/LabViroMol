namespace LabViroMol.Modules.Identity.Contracts;

public record UserInfo(
    string FirstName,
    string LastName,
    string? PhoneNumber,
    string? EmergencyContactName,
    string? EmergencyContactNumber,
    ResearchRegistrationData? ResearchData);
