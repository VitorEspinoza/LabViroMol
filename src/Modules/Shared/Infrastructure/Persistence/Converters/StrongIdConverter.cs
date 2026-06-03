using System;
using System.Linq.Expressions;
using LabViroMol.Modules.Shared.Kernel.Primitives;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace LabViroMol.Modules.Shared.Infrastructure.Persistence.Converters;

public class StrongIdConverter<TId> : ValueConverter<TId, Guid>
    where TId : struct, IStrongId<TId>
{
    public StrongIdConverter() 
        : base(
            id => id.Value,          
            CreateFromGuidExpression() 
        )
    {
    }

    private static Expression<Func<Guid, TId>> CreateFromGuidExpression()
    {
        var parameter = Expression.Parameter(typeof(Guid), "guid");

        var method = typeof(TId).GetMethod(nameof(IStrongId<TId>.From), [typeof(Guid)])
                     ?? throw new InvalidOperationException($"Método From não encontrado em {typeof(TId).Name}");

        var call = Expression.Call(null, method, parameter);

        return Expression.Lambda<Func<Guid, TId>>(call, parameter);
    }
}