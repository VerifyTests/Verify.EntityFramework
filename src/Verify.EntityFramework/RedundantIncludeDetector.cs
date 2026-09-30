using UnaryExpression = System.Linq.Expressions.UnaryExpression;

// Finds an Include that does nothing:
// - one that ends in an owned navigation, since EF always loads an owned type with its owner
// - one that ends in a navigation configured with AutoInclude, unless the query uses IgnoreAutoIncludes
// - one whose path is the same as, or the start of, another path, which EF merges
// A path is an Include and the ThenIncludes after it, for example Include(_ => _.Company).ThenInclude(_ => _.Employees),
// or Include(_ => _.Company.Employees).
// The check leans towards not throwing: a string Include, or a path that can not be resolved to navigations, skips the
// check, and a filtered path is never reported, since removing it would remove its filter.
static class RedundantIncludeDetector
{
    public static void ThrowIfRedundant(Expression query, IModel model)
    {
        // the operators applied to the query, from the root outwards
        var calls = new List<MethodCallExpression>();
        var expression = query;
        while (expression is MethodCallExpression { Method.IsStatic: true, Arguments.Count: > 0 } call)
        {
            calls.Add(call);
            expression = call.Arguments[0];
        }

        calls.Reverse();

        var ignoresAutoIncludes = false;
        var paths = new List<IncludePath>();
        foreach (var call in calls)
        {
            var method = call.Method;
            if (method.DeclaringType != typeof(EntityFrameworkQueryableExtensions))
            {
                continue;
            }

            if (method.Name == nameof(EntityFrameworkQueryableExtensions.IgnoreAutoIncludes))
            {
                ignoresAutoIncludes = true;
                continue;
            }

            var isInclude = method.Name == nameof(EntityFrameworkQueryableExtensions.Include);
            if (!isInclude &&
                method.Name != nameof(EntityFrameworkQueryableExtensions.ThenInclude))
            {
                continue;
            }

            // a string Include can not be checked without resolving the path
            if (call.Arguments[1].Unquote() is not LambdaExpression lambda)
            {
                return;
            }

            var parameter = lambda.Parameters[0];
            IEntityType? entityType;
            IncludePath path;
            if (isInclude)
            {
                entityType = model.FindEntityType(parameter.Type);
                path = new();
                paths.Add(path);
            }
            else
            {
                if (paths.Count == 0)
                {
                    return;
                }

                path = paths[^1];
                entityType = WithClrType(path.Segments[^1].Navigation.TargetEntityType, parameter.Type);
            }

            if (entityType == null ||
                !path.Append(call, lambda.Body, parameter, entityType))
            {
                return;
            }
        }

        foreach (var path in paths)
        {
            ThrowIfIgnored(path, ignoresAutoIncludes);
        }

        for (var index = 0; index < paths.Count; index++)
        {
            ThrowIfCovered(paths, index);
        }
    }

    static void ThrowIfIgnored(IncludePath path, bool ignoresAutoIncludes)
    {
        var last = path.Segments[^1];
        if (last.Filtered)
        {
            return;
        }

        string reason;
        var navigation = last.Navigation;
        if (navigation is INavigation { ForeignKey.IsOwnership: true, IsOnDependent: false })
        {
            reason = $"{navigation.Name} is owned, and EF always loads an owned type with its owner";
        }
        else if (!ignoresAutoIncludes &&
                 navigation.IsEagerLoaded)
        {
            reason = $"{navigation.Name} is configured with AutoInclude, so EF already loads it";
        }
        else
        {
            return;
        }

        var fix = "Remove it.";
        if (path.Segments.Count > 1)
        {
            fix = $"Remove {navigation.Name} from the path, so it ends at {path.Segments[^2].Navigation.Name}.";
        }

        throw new(
            $"""
             {path} is ignored, since {reason}.
             {fix}
             """);
    }

    // the path is covered by another that starts with the same navigations, or by an earlier identical path
    static void ThrowIfCovered(List<IncludePath> paths, int index)
    {
        var path = paths[index];
        if (path.Segments.Any(_ => _.Filtered))
        {
            return;
        }

        for (var otherIndex = 0; otherIndex < paths.Count; otherIndex++)
        {
            var other = paths[otherIndex];
            if (otherIndex == index ||
                other.Segments.Count < path.Segments.Count ||
                (other.Segments.Count == path.Segments.Count && otherIndex > index) ||
                !path.IsStartOf(other))
            {
                continue;
            }

            var navigations = string.Join('.', path.Segments.Select(_ => _.Navigation.Name));
            throw new(
                $"""
                 {path} is redundant, since {other} also includes {navigations}.
                 Remove it.
                 """);
        }
    }

    // the entity type of a lambda parameter, which can be a type derived from the navigation's target
    static IEntityType? WithClrType(IEntityType entityType, Type type) =>
        entityType
            .GetDerivedTypesInclusive()
            .FirstOrDefault(_ => _.ClrType == type);

    class IncludePath
    {
        List<MethodCallExpression> calls = [];

        public List<Segment> Segments { get; } = [];

        // adds the navigations in the body of an Include or ThenInclude, for example _.Company.Employees
        public bool Append(MethodCallExpression call, Expression body, ParameterExpression parameter, IEntityType entityType)
        {
            calls.Add(call);

            // a filtered Include, for example _.Employees.Where(...).OrderBy(...)
            var filtered = false;
            while (body is MethodCallExpression { Method.IsStatic: true, Arguments.Count: > 0 } filter &&
                   (filter.Method.DeclaringType == typeof(Enumerable) ||
                    filter.Method.DeclaringType == typeof(Queryable)))
            {
                filtered = true;
                body = filter.Arguments[0];
            }

            var count = Segments.Count;
            if (Resolve(body, parameter, entityType) == null ||
                Segments.Count == count)
            {
                return false;
            }

            if (filtered)
            {
                Segments[^1] = Segments[^1] with
                {
                    Filtered = true
                };
            }

            return true;
        }

        // the entity type the expression evaluates to, adding a segment for each navigation
        IEntityType? Resolve(Expression expression, ParameterExpression parameter, IEntityType parameterType)
        {
            switch (expression)
            {
                case ParameterExpression when expression == parameter:
                    return parameterType;
                // a cast to a derived type, for example ((Manager) _).Reports
                case UnaryExpression
                {
                    NodeType: ExpressionType.Convert or ExpressionType.TypeAs
                } unary:
                {
                    var operand = Resolve(unary.Operand, parameter, parameterType);
                    if (operand == null)
                    {
                        return null;
                    }

                    if (unary.Type.IsAssignableFrom(operand.ClrType))
                    {
                        return operand;
                    }

                    return WithClrType(operand, unary.Type);
                }
                case MemberExpression { Expression: { } owner } member:
                {
                    var ownerType = Resolve(owner, parameter, parameterType);
                    if (ownerType == null)
                    {
                        return null;
                    }

                    var name = member.Member.Name;
                    INavigationBase? navigation = ownerType.FindNavigation(name);
                    navigation ??= ownerType.FindSkipNavigation(name);
                    if (navigation == null)
                    {
                        return null;
                    }

                    Segments.Add(new(navigation, false));
                    return navigation.TargetEntityType;
                }
                default:
                    return null;
            }
        }

        public bool IsStartOf(IncludePath other)
        {
            for (var index = 0; index < Segments.Count; index++)
            {
                if (Segments[index].Navigation != other.Segments[index].Navigation)
                {
                    return false;
                }
            }

            return true;
        }

        public override string ToString() =>
            string.Join('.', calls.Select(_ => _.Describe()));
    }

    record Segment(INavigationBase Navigation, bool Filtered);
}
