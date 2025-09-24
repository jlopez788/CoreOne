using CoreOne.Operations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreOne.Extensions;

public static class QueryExtensions
{
    private class OrderByInfo(string name, SortDirection direction, bool initial) : OrderBy(name, direction)
    {
        public bool Initial { get; } = initial;
    }

    private static readonly Lazy<Data<string, MethodInfo>> Methods = new(InitializeMethods);

    public static IQueryable<TSource> OrderBy<TSource>(this IQueryable<TSource> query, IEnumerable<OrderBy> orderBy)
    {
        return orderBy.Select((p, i) => new OrderByInfo(p.Field, p.Direction, i == 0))
            .Aggregate(query, (next, order) => ApplyOrderBy(next, order));
    }

    public static IQueryable<TSource> OrderBy<TSource>(this IQueryable<TSource> query, string orderBy) => ParseOrderBy(orderBy)
            .Aggregate(query, (next, order) => ApplyOrderBy(query, order));

    public static IQueryable<TSource> Paginate<TSource>(this IQueryable<TSource> source, PageRequest request)
    {
        return request.PageSize == 0 ? source : source.Skip((request.CurrentPage - 1) * request.PageSize)
                      .Take(request.PageSize);
    }

    private static IQueryable<T> ApplyOrderBy<T>(IQueryable<T> query, OrderByInfo orderByInfo)
    {
        var props = orderByInfo.Field.Split('.');
        var type = typeof(T);

        var arg = Expression.Parameter(type, "x");
        Expression expr = arg;
        foreach (var prop in props)
        {
            var pi = type.GetProperty(prop);
            if (pi is not null)
            {
                expr = Expression.Property(expr, pi);
                type = pi.PropertyType;
            }
        }
        var delegateType = typeof(Func<,>).MakeGenericType(typeof(T), type);
        var lambda = Expression.Lambda(delegateType, expr, arg);
        var methodName = string.Empty;

        methodName = !orderByInfo.Initial && query is IOrderedQueryable<T>
            ? orderByInfo.Direction == SortDirection.Ascending ? nameof(Queryable.ThenBy) : nameof(Queryable.ThenByDescending)
            : orderByInfo.Direction == SortDirection.Ascending ? nameof(Queryable.OrderBy) : nameof(Queryable.OrderByDescending);

        var method = Methods.Value.Get(methodName)?.MakeGenericMethod(typeof(T), type);
        return (IQueryable<T>)(method?.Invoke(null, [query, lambda]) ?? query);
    }

    private static Data<string, MethodInfo> InitializeMethods()
    {
        var data = new Data<string, MethodInfo>(MStringComparer.OrdinalIgnoreCase);
        var names = (new string[] {
        nameof(Queryable.ThenBy),
        nameof(Queryable.ThenByDescending),
        nameof(Queryable.OrderBy),
        nameof(Queryable.OrderByDescending) })
            .ToHashSet(MStringComparer.OrdinalIgnoreCase);
        typeof(Queryable).GetMethods()
            .Where(method => names.Contains(method.Name)
                     && method.IsGenericMethodDefinition
                     && method.GetGenericArguments().Length == 2
                     && method.GetParameters().Length == 2)
            .Each(p => data.Set(p.Name, p));
        return data;
    }

    private static IEnumerable<OrderByInfo> ParseOrderBy(string orderBy)
    {
        if (string.IsNullOrEmpty(orderBy))
            yield break;

        var items = orderBy.Split(',');
        var initial = true;
        foreach (var item in items)
        {
            var pair = item.Trim().Split(' ');

            if (pair.Length > 2)
                throw new ArgumentException(string.Format("Invalid OrderBy string '{0}'. Order By Format: Property, Property2 ASC, Property2 DESC", item));

            var prop = pair[0].Trim();
            if (string.IsNullOrEmpty(prop))
                throw new ArgumentException("Invalid Property. Order By Format: Property, Property2 ASC, Property2 DESC");

            var dir = SortDirection.Ascending;
            if (pair.Length == 2)
                dir = pair[1].MatchesAny("Ascending", "ASC") ? SortDirection.Ascending : SortDirection.Descending;

            yield return new OrderByInfo(prop, dir, initial);

            initial = false;
        }
    }
}