namespace LabViroMol.Modules.Identity.Application.Users.ViewModels;

public record RoleDetailViewModel(Guid Id, string Name, List<string> Permissions);
