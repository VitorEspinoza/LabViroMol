using System;
using System.Linq.Expressions;
using LabViroMol.Modules.Shared.Kernel.Identity;
using LabViroMol.Modules.Shared.Kernel.Primitives;
using Microsoft.EntityFrameworkCore;

namespace LabViroMol.Modules.Shared.Infrastructure.Extensions;

public static class ModelBuilderExtensions
{
    public static void ApplyPersistenceConfigs(this ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            var clrType = entityType.ClrType;

            if (typeof(ICreationAuditable).IsAssignableFrom(clrType))
            {
                modelBuilder.Entity(clrType).Property<DateTimeOffset>("CreatedAt");
                modelBuilder.Entity(clrType).Property<UserId>("CreatedBy").HasMaxLength(15);
            }

            if (typeof(IModificationAuditable).IsAssignableFrom(clrType))
            {
                modelBuilder.Entity(clrType).Property<DateTimeOffset?>("UpdatedAt").IsRequired(false);
                modelBuilder.Entity(clrType).Property<UserId?>("UpdatedBy").IsRequired(false).HasMaxLength(15);
            }

            if (typeof(IDeletionAuditable).IsAssignableFrom(clrType))
            {
                modelBuilder.Entity(clrType).Property<bool>("IsDeleted");
                modelBuilder.Entity(clrType).Property<DateTimeOffset?>("RemovedAt").IsRequired(false);
                modelBuilder.Entity(clrType).Property<UserId?>("RemovedBy").IsRequired(false).HasMaxLength(15);

                var parameter = Expression.Parameter(clrType, "e");
                var efPropertyMethod = typeof(EF).GetMethod(nameof(EF.Property))!.MakeGenericMethod(typeof(bool));
                var property = Expression.Call(efPropertyMethod, parameter, Expression.Constant("IsDeleted"));
                var falseConstant = Expression.Constant(false);
                var comparison = Expression.Equal(property, falseConstant);
                var lambda = Expression.Lambda(comparison, parameter);

                modelBuilder.Entity(clrType).HasQueryFilter(lambda);
            }

            if (typeof(IConcurrencySafe).IsAssignableFrom(clrType))
            {
                modelBuilder.Entity(clrType)
                    .Property<byte[]>("RowVersion")
                    .IsRowVersion()
                    .IsConcurrencyToken();
            }
        }
    }
}
