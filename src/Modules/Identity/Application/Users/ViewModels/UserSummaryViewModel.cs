namespace LabViroMol.Modules.Identity.Application.Users.ViewModels;

public record UserSummaryViewModel(Guid Id, string FullName, string Email, bool IsActive, List<string> Roles, string? EmergencyContactName, string? EmergencyContactNumber);
