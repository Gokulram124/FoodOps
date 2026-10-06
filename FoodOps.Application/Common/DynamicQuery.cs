using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;

namespace FoodOps.Application.Common;

public static class DynamicQuery
{
    private const BindingFlags Flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase;

    // filter format:  field:op:value,field:op:value   (ops: eq neq gt gte lt lte contains)
    public static IQueryable<T> ApplyFilter<T>(this IQueryable<T> source, string? filter, IReadOnlySet<string> allowed)
    {
        if (string.IsNullOrWhiteSpace(filter)) return source;

        foreach (var part in filter.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var seg = part.Split(':', 3);
            if (seg.Length != 3)
                throw new ArgumentException($"Invalid filter '{part}'. Use field:op:value.");

            var op = seg[1].ToLowerInvariant();
            var prop = typeof(T).GetProperty(seg[0], Flags);
            if (prop is null || !allowed.Contains(prop.Name))
                throw new ArgumentException($"Cannot filter by '{seg[0]}'.");

            var targetType = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;
            object value;
            try { value = Convert.ChangeType(seg[2], targetType, CultureInfo.InvariantCulture); }
            catch { throw new ArgumentException($"'{seg[2]}' is not a valid value for '{prop.Name}'."); }

            var x = Expression.Parameter(typeof(T), "x");
            var member = Expression.Property(x, prop);
            var constant = Expression.Constant(value, prop.PropertyType);

            Expression body;
            try
            {
                body = op switch
                {
                    "eq" => Expression.Equal(member, constant),
                    "neq" => Expression.NotEqual(member, constant),
                    "gt" => Expression.GreaterThan(member, constant),
                    "gte" => Expression.GreaterThanOrEqual(member, constant),
                    "lt" => Expression.LessThan(member, constant),
                    "lte" => Expression.LessThanOrEqual(member, constant),
                    "contains" when prop.PropertyType == typeof(string)
                          => Expression.Call(member, nameof(string.Contains), Type.EmptyTypes, constant),
                    _ => throw new ArgumentException($"Operator '{op}' is not supported for '{prop.Name}'.")
                };
            }
            catch (InvalidOperationException)
            {
                throw new ArgumentException($"Operator '{op}' is not supported for '{prop.Name}'.");
            }

            source = source.Where(Expression.Lambda<Func<T, bool>>(body, x));
        }
        return source;
    }

    // sort format:  -rating,name   ('-' = descending)
    public static IQueryable<T> ApplySort<T>(this IQueryable<T> source, string? sort,
        IReadOnlySet<string> allowed, string tieBreaker = "Id")
    {
        var items = new List<(string Name, bool Desc)>();
        if (!string.IsNullOrWhiteSpace(sort))
            foreach (var part in sort.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                items.Add((part.TrimStart('-', '+'), part.StartsWith('-')));
        items.Add((tieBreaker, false));   // stable paging

        var current = source;
        var first = true;
        foreach (var (name, desc) in items)
        {
            var prop = typeof(T).GetProperty(name, Flags);
            if (prop is null || (!allowed.Contains(prop.Name) && prop.Name != tieBreaker))
                throw new ArgumentException($"Cannot sort by '{name}'.");

            var x = Expression.Parameter(typeof(T), "x");
            var lambda = Expression.Lambda(Expression.Property(x, prop), x);
            var method = first ? (desc ? "OrderByDescending" : "OrderBy")
                               : (desc ? "ThenByDescending" : "ThenBy");

            var call = Expression.Call(typeof(Queryable), method,
                new[] { typeof(T), prop.PropertyType }, current.Expression, Expression.Quote(lambda));
            current = current.Provider.CreateQuery<T>(call);
            first = false;
        }
        return current;
    }
}