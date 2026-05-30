using LabViroMol.Modules.Shared.Kernel.Primitives;

namespace LabViroMol.Modules.Identity.Domain.Users;

public record UserName
{
    public string FirstName { get; init; }
    public string LastName { get; init; }

    public UserName(string firstName, string lastName)
    {
        Guard.AgainstEmpty(firstName, "O primeiro nome é obrigatório.");
        Guard.AgainstEmpty(lastName, "O sobrenome é obrigatório.");
        FirstName = firstName.Trim();
        LastName = lastName.Trim();
    }

    public string FullName => $"{FirstName} {LastName}";
}
