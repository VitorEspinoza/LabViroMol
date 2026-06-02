using Microsoft.AspNetCore.Identity;

namespace LabViroMol.Modules.Identity.Infrastructure.Identity;

public class PortugueseIdentityErrorDescriber : IdentityErrorDescriber
{
    public override IdentityError DefaultError()
        => new() { Code = "DefaultError", Description = "Ocorreu um erro desconhecido." };

    public override IdentityError ConcurrencyFailure()
        => new() { Code = "ConcurrencyFailure", Description = "Falha de concorrência. O registro foi modificado por outro processo." };

    public override IdentityError PasswordMismatch()
        => new() { Code = "PasswordMismatch", Description = "Senha incorreta." };

    public override IdentityError InvalidToken()
        => new() { Code = "InvalidToken", Description = "Token inválido." };

    public override IdentityError RecoveryCodeRedemptionFailed()
        => new() { Code = "RecoveryCodeRedemptionFailed", Description = "Falha ao resgatar o código de recuperação." };

    public override IdentityError LoginAlreadyAssociated()
        => new() { Code = "LoginAlreadyAssociated", Description = "Já existe um login externo associado." };

    public override IdentityError InvalidUserName(string? userName)
        => new() { Code = "InvalidUserName", Description = $"O nome de usuário '{userName}' é inválido." };

    public override IdentityError InvalidEmail(string? email)
        => new() { Code = "InvalidEmail", Description = $"O e-mail '{email}' é inválido." };

    public override IdentityError DuplicateUserName(string userName)
        => new() { Code = "DuplicateUserName", Description = $"O nome de usuário '{userName}' já está em uso." };

    public override IdentityError DuplicateEmail(string email)
        => new() { Code = "DuplicateEmail", Description = $"O e-mail '{email}' já está em uso." };

    public override IdentityError InvalidRoleName(string? role)
        => new() { Code = "InvalidRoleName", Description = $"O nome do perfil '{role}' é inválido." };

    public override IdentityError DuplicateRoleName(string role)
        => new() { Code = "DuplicateRoleName", Description = $"O perfil '{role}' já existe." };

    public override IdentityError UserAlreadyHasPassword()
        => new() { Code = "UserAlreadyHasPassword", Description = "O usuário já possui uma senha definida." };

    public override IdentityError UserLockoutNotEnabled()
        => new() { Code = "UserLockoutNotEnabled", Description = "O bloqueio não está habilitado para este usuário." };

    public override IdentityError UserAlreadyInRole(string role)
        => new() { Code = "UserAlreadyInRole", Description = $"O usuário já possui o perfil '{role}'." };

    public override IdentityError UserNotInRole(string role)
        => new() { Code = "UserNotInRole", Description = $"O usuário não possui o perfil '{role}'." };

    public override IdentityError PasswordTooShort(int length)
        => new() { Code = "PasswordTooShort", Description = $"A senha deve ter no mínimo {length} caracteres." };

    public override IdentityError PasswordRequiresUniqueChars(int uniqueChars)
        => new() { Code = "PasswordRequiresUniqueChars", Description = $"A senha deve conter pelo menos {uniqueChars} caracteres distintos." };

    public override IdentityError PasswordRequiresNonAlphanumeric()
        => new() { Code = "PasswordRequiresNonAlphanumeric", Description = "A senha deve conter pelo menos um caractere especial." };

    public override IdentityError PasswordRequiresDigit()
        => new() { Code = "PasswordRequiresDigit", Description = "A senha deve conter pelo menos um dígito ('0'-'9')." };

    public override IdentityError PasswordRequiresLower()
        => new() { Code = "PasswordRequiresLower", Description = "A senha deve conter pelo menos uma letra minúscula ('a'-'z')." };

    public override IdentityError PasswordRequiresUpper()
        => new() { Code = "PasswordRequiresUpper", Description = "A senha deve conter pelo menos uma letra maiúscula ('A'-'Z')." };
}
