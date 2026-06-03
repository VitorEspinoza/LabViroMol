using System;
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
        string? emergencyContactNumber) : base(id)
    {
        Name = name;
        Email = email;
        PhoneNumber = phoneNumber;
        EmergencyContactNumber = emergencyContactNumber;
    }

    public UserName Name { get; private set; }
    public Email Email { get; private set; }
    public string? PhoneNumber { get; private set; }
    public string? EmergencyContactNumber { get; private set; }
    public DateTimeOffset? DeactivatedAt { get; private set; }
    public bool IsActive => DeactivatedAt == null;

    public static User Create(
        UserId id,
        UserName name,
        Email email,
        string? phoneNumber,
        string? emergencyContactNumber)
    {
        return new User(
            id,
            name,
            email,
            phoneNumber?.Trim(),
            emergencyContactNumber?.Trim());
    }

    public void Update(
        UserName name,
        Email email,
        string? phoneNumber,
        string? emergencyContactNumber)
    {
        Name = name;
        Email = email;
        PhoneNumber = phoneNumber?.Trim();
        EmergencyContactNumber = emergencyContactNumber?.Trim();
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
