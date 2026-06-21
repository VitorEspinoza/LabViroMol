using System.Reflection;
using LabViroMol.Modules.Identity.Application.Roles.Queries;
using LabViroMol.Modules.Shared.Kernel.Authorization;

namespace LabViroMol.Modules.Identity.Infrastructure.Roles;

public class PermissionQueries : IPermissionQueries
{
    private static readonly Lazy<IReadOnlyCollection<string>> _permissions = new(() =>
        typeof(Permissions)
            .GetNestedTypes(BindingFlags.Public | BindingFlags.Static)
            .SelectMany(t => t.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy))
            .Where(f => f.IsLiteral && !f.IsInitOnly && f.FieldType == typeof(string))
            .Select(f => (string)f.GetRawConstantValue()!)
            .ToList());

    public IReadOnlyCollection<string> GetAll() => _permissions.Value;
}
