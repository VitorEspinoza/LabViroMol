using System.Linq.Expressions;

namespace LabViroMol.Modules.Shared.Kernel.Pagination;

public static class QueryableSearchExtensions
{
    public static IQueryable<T> WhereSearch<T>(
        this IQueryable<T> source, string? search, params Expression<Func<T, string?>>[] fields)
    {
        if (string.IsNullOrWhiteSpace(search) || fields.Length == 0)
            return source;

        var containsMethod = typeof(string).GetMethod(nameof(string.Contains), [typeof(string)])!;
        var parameter = Expression.Parameter(typeof(T), "x");
        var searchExpr = Expression.Constant(search);
        var nullString = Expression.Constant(null, typeof(string));

        Expression? body = null;
        foreach (var field in fields)
        {
            var rebound = new ParameterRebinder(field.Parameters[0], parameter).Visit(field.Body)!;
            var notNull = Expression.NotEqual(rebound, nullString);
            var contains = Expression.Call(rebound, containsMethod, searchExpr);
            var guarded = Expression.AndAlso(notNull, contains);
            body = body is null ? guarded : Expression.OrElse(body, guarded);
        }

        return source.Where(Expression.Lambda<Func<T, bool>>(body!, parameter));
    }

    private sealed class ParameterRebinder(ParameterExpression source, ParameterExpression target) : ExpressionVisitor
    {
        protected override Expression VisitParameter(ParameterExpression node) => node == source ? target : base.VisitParameter(node);
    }
}
