// In `Include(_ => _.Employees).Where(_ => _.Employees.Any(e => e.Age > 30))` the Where filters the companies, but
// the Include still loads every employee of each company returned. That is often meant as a filtered Include:
// `Include(_ => _.Employees.Where(e => e.Age > 30))`. Opt in, since filtering the parents by their children is also a
// correct query.
static class CollectionFilterOutsideIncludeDetector
{
    public static void ThrowIfFilteredOutside(Expression query)
    {
        // the operators applied to the query, from the outside in
        var calls = new List<MethodCallExpression>();
        var expression = query;
        while (expression is MethodCallExpression { Method.IsStatic: true, Arguments.Count: > 0 } call)
        {
            calls.Add(call);
            expression = call.Arguments[0];
        }

        var included = new Dictionary<string, MethodCallExpression>();
        foreach (var call in calls)
        {
            if (call.Method.DeclaringType == typeof(EntityFrameworkQueryableExtensions) &&
                call.Method.Name == nameof(EntityFrameworkQueryableExtensions.Include) &&
                IncludedCollection(call) is { } name)
            {
                included.TryAdd(name, call);
            }
        }

        if (included.Count == 0)
        {
            return;
        }

        foreach (var call in calls)
        {
            if (call.Method.DeclaringType != typeof(Queryable) ||
                call.Method.Name != nameof(Queryable.Where) ||
                call.Arguments[1].Unquote() is not LambdaExpression { Parameters.Count: 1 } predicate)
            {
                continue;
            }

            var filtered = FilteredCollection(predicate, included.Keys);
            if (filtered == null)
            {
                continue;
            }

            var include = included[filtered];
            throw new($"{call.Describe()} filters by {filtered}, but {include.Describe()} still loads all {filtered} of the rows returned. To load only the matching {filtered}, filter inside the Include, for example Include(_ => _.{filtered}.Where(...)). If filtering the rows by {filtered} is intended, allow it with ThrowOnCollectionFilterOutsideInclude = false.");
        }
    }

    // the collection navigation named directly on the root, for example Employees in Include(_ => _.Employees). A
    // filtered Include already limits what it loads, so is not matched.
    static string? IncludedCollection(MethodCallExpression include)
    {
        if (include.Arguments[1].Unquote() is not LambdaExpression lambda ||
            lambda.Body is not MemberExpression member ||
            member.Expression != lambda.Parameters[0] ||
            member.Type == typeof(string) ||
            !typeof(IEnumerable).IsAssignableFrom(member.Type))
        {
            return null;
        }

        return member.Member.Name;
    }

    // the included collection that the predicate reads, for example Employees in _.Employees.Any(...)
    static string? FilteredCollection(LambdaExpression predicate, IEnumerable<string> included)
    {
        var finder = new MemberFinder(predicate.Parameters[0], included.ToHashSet());
        finder.Visit(predicate.Body);
        return finder.Found;
    }

    class MemberFinder(ParameterExpression parameter, HashSet<string> names) :
        ExpressionVisitor
    {
        public string? Found { get; private set; }

        protected override Expression VisitMember(MemberExpression node)
        {
            if (Found == null &&
                node.Expression == parameter &&
                names.Contains(node.Member.Name))
            {
                Found = node.Member.Name;
                return node;
            }

            return base.VisitMember(node);
        }
    }
}
