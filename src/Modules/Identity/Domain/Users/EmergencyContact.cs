using LabViroMol.Modules.Shared.Kernel.Primitives;

namespace LabViroMol.Modules.Identity.Domain.Users;

public record EmergencyContact
{
    public string Name { get; init; }
    public string Number { get; init; }

    public EmergencyContact(string name, string number)
    {
        Guard.AgainstEmpty(name, "O nome do contato de emergência é obrigatório.");
        Guard.AgainstEmpty(number, "O número do contato de emergência é obrigatório.");
        Name = name.Trim();
        Number = number.Trim();
    }

    public static EmergencyContact? FromNullable(string? name, string? number)
        => string.IsNullOrWhiteSpace(name) && string.IsNullOrWhiteSpace(number)
            ? null
            : new EmergencyContact(name!, number!);
}
