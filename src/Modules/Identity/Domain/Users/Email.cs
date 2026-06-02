using LabViroMol.Modules.Shared.Kernel.Primitives;

namespace LabViroMol.Modules.Identity.Domain.Users;

public record Email
{
    public string Value { get; init; }

    public Email(string value)
    {
        Guard.AgainstEmpty(value, "O e-mail é obrigatório.");

        if (!value.Contains('@'))
            throw new DomainException("Formato de e-mail inválido.");

        Value = value.Trim().ToLowerInvariant();
    }

    public override string ToString() => Value;
}
