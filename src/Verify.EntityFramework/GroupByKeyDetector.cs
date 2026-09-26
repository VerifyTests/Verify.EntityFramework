// `GroupBy(_ => _.CompanyId).Select(_ => _.Key)` only returns the distinct keys, which
// `Select(_ => _.CompanyId).Distinct()` states directly. Also matches the GroupBy overload with a result selector that
// ignores the elements. A selector that uses the group, for example `_.Count()`, needs the groups, so is not matched.
class GroupByKeyDetector :
    ExpressionVisitor
{
    static GroupByKeyDetector instance = new();

    public static void ThrowIfOnlyKey(Expression query) =>
        instance.Visit(query);

    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        var method = node.Method;
        if (IsLinq(method))
        {
            var arguments = node.Arguments;
            // GroupBy(source, key).Select(source, group => ...)
            if (method.Name == nameof(Queryable.Select) &&
                arguments[0] is
                    MethodCallExpression
                    {
                        Method.Name: nameof(Queryable.GroupBy),
                        Arguments.Count: 2
                    } groupBy &&
                IsLinq(groupBy.Method) &&
                arguments[1].Unquote()
                    is LambdaExpression
                    {
                        Parameters.Count: 1
                    } selector &&
                OnlyUsesKey(selector.Body, selector.Parameters[0]))
            {
                Throw(groupBy);
            }

            // GroupBy(source, key, (key, elements) => ...)
            if (method.Name == nameof(Queryable.GroupBy) &&
                arguments.Count == 3 &&
                arguments[2].Unquote()
                    is LambdaExpression
                    {
                        Parameters.Count: 2
                    } resultSelector &&
                !Uses(resultSelector.Body, resultSelector.Parameters[1]))
            {
                Throw(node);
            }
        }

        return base.VisitMethodCall(node);
    }

    static void Throw(MethodCallExpression groupBy)
    {
        var key = groupBy.Arguments[1].Unquote();
        throw new($"GroupBy({key}) only returns the distinct keys, since the groups are only used for their Key. Use Select({key}).Distinct(), which states that directly.");
    }

    static bool IsLinq(MethodInfo method) =>
        method.DeclaringType == typeof(Queryable) ||
        method.DeclaringType == typeof(Enumerable);

    // every use of the group is `group.Key`, and there is at least one
    static bool OnlyUsesKey(Expression body, ParameterExpression group)
    {
        var finder = new UseFinder(group);
        finder.Visit(body);
        return finder is
        {
            KeyUses: > 0,
            OtherUses: 0
        };
    }

    static bool Uses(Expression body, ParameterExpression parameter)
    {
        var finder = new UseFinder(parameter);
        finder.Visit(body);
        return finder.KeyUses + finder.OtherUses > 0;
    }

    class UseFinder(ParameterExpression parameter) :
        ExpressionVisitor
    {
        public int KeyUses { get; private set; }
        public int OtherUses { get; private set; }

        protected override Expression VisitMember(MemberExpression node)
        {
            if (node.Expression == parameter &&
                node.Member.Name == "Key")
            {
                KeyUses++;
                return node;
            }

            return base.VisitMember(node);
        }

        protected override Expression VisitParameter(ParameterExpression node)
        {
            if (node == parameter)
            {
                OtherUses++;
            }

            return node;
        }
    }
}
