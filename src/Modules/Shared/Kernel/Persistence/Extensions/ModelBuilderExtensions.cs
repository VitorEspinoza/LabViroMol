using System.Linq.Expressions;
using LabViroMol.Modules.Shared.Abstractions.Primitives;
using Microsoft.EntityFrameworkCore;

namespace Kernel.Extensions;

public static class ModelBuilderExtensions
{
    public static void ApplyLabViroMolConfigurations(this ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (!typeof(AuditableEntity<>).IsAssignableFrom(entityType.ClrType)) continue;
            
            var parameter = Expression.Parameter(entityType.ClrType, "e");
            var property = Expression.Property(parameter, nameof(AuditableEntity<>.IsDeleted));
            var falseConstant = Expression.Constant(false);
            var comparison = Expression.Equal(property, falseConstant);
            var lambda = Expression.Lambda(comparison, parameter);

            modelBuilder.Entity(entityType.ClrType).HasQueryFilter(lambda);

        }
    }
}