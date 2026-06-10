using System.Reflection;
using FluentValidation;
using LabViroMol.Modules.Shared.Kernel.Authorization;

namespace LabViroMol.Modules.Identity.Application.Roles.UpdateRolePermissions;

public class UpdateRolePermissionsCommandValidator : AbstractValidator<UpdateRolePermissionsCommand>
{
    private static readonly HashSet<string> ValidPermissions = GetAllPermissions();

    public UpdateRolePermissionsCommandValidator()
    {
        RuleFor(x => x.RoleId)
            .NotEmpty().WithMessage("O identificador do perfil é obrigatório.");

        RuleForEach(x => x.Permissions)
            .Must(p => ValidPermissions.Contains(p))
            .WithMessage("Permissão '{PropertyValue}' não é válida.");
    }

    private static HashSet<string> GetAllPermissions()
    {
        return typeof(Permissions)
            .GetNestedTypes(BindingFlags.Public | BindingFlags.Static)
            .SelectMany(t => t.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy))
            .Where(f => f.IsLiteral && !f.IsInitOnly && f.FieldType == typeof(string))
            .Select(f => (string)f.GetRawConstantValue()!)
            .ToHashSet();
    }
}
