using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using FluentValidation;
using LabViroMol.Modules.Shared.Kernel.Authorization;

namespace LabViroMol.Modules.Identity.Application.Roles.CreateRole;

public class CreateRoleCommandValidator : AbstractValidator<CreateRoleCommand>
{
    private static readonly HashSet<string> ValidPermissions = GetAllPermissions();

    public CreateRoleCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Nome do perfil é obrigatório.")
            .MinimumLength(2).WithMessage("Nome deve ter no mínimo 2 caracteres.");

        RuleFor(x => x.Permissions)
            .NotEmpty().WithMessage("Ao menos uma permissão é obrigatória.");

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
