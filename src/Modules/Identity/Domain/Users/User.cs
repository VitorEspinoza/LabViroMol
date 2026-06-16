using LabViroMol.Modules.Shared.Kernel.Identity;
using LabViroMol.Modules.Shared.Kernel.Primitives;

namespace LabViroMol.Modules.Identity.Domain.Users;

public class User : AggregateRoot<UserId>, ICreationAuditable, IModificationAuditable
{
    private User() { }

    private User(
        UserId id,
        UserName name,
        Email email,
        string? phoneNumber,
        EmergencyContact? emergencyContact) : base(id)
    {
        Name = name;
        Email = email;
        PhoneNumber = phoneNumber;
        EmergencyContact = emergencyContact;
    }

    public UserName Name { get; private set; }
    public Email Email { get; private set; }
    public string? PhoneNumber { get; private set; }
    public EmergencyContact? EmergencyContact { get; private set; }
    public DateTimeOffset? DeactivatedAt { get; private set; }
    public bool IsActive => DeactivatedAt == null;

    public static User Create(
        UserId id,
        UserName name,
        Email email,
        string? phoneNumber,
        EmergencyContact? emergencyContact)
    {
        EnsureEmergencyContactDifferent(phoneNumber, emergencyContact);
        return new User(id, name, email, phoneNumber?.Trim(), emergencyContact);
    }

    public void Update(
        UserName name,
        Email email,
        string? phoneNumber,
        EmergencyContact? emergencyContact)
    {
        EnsureEmergencyContactDifferent(phoneNumber, emergencyContact);
        Name = name;
        Email = email;
        PhoneNumber = phoneNumber?.Trim();
        EmergencyContact = emergencyContact;
    }

    private static void EnsureEmergencyContactDifferent(string? phoneNumber, EmergencyContact? emergencyContact)
    {
        if (emergencyContact is null || string.IsNullOrWhiteSpace(phoneNumber))
            return;

        var phone = new string(phoneNumber.Where(char.IsDigit).ToArray());
        var emergency = new string(emergencyContact.Number.Where(char.IsDigit).ToArray());

        if (phone.Length > 0 && phone == emergency)
            throw new DomainException("O contato de emergência não pode ter o mesmo número que o usuário.");
    }

    public void Deactivate()
    {
        if (!IsActive)
            return;
        DeactivatedAt = DateTimeOffset.UtcNow;
    }

    public void Reactivate()
    {
        if (IsActive)
            throw new DomainException("O usuário já está ativo.");
        DeactivatedAt = null;
    }
}
