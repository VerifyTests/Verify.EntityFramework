using UnaryExpression = System.Linq.Expressions.UnaryExpression;

// Distinct is redundant when each row comes from a single entity, and includes that entity's primary key, since the
// key already makes each row unique. Only sources that return each entity once are matched: an entity set, or a
// collection navigation, followed by operators like Where, OrderBy, and Take. A Join, SelectMany, GroupBy, raw SQL, or
// temporal query can return an entity more than once, so is not matched.
class RedundantDistinctDetector(IModel model) :
    ExpressionVisitor
{
    public static void ThrowIfRedundant(Expression query, IModel model) =>
        new RedundantDistinctDetector(model).Visit(query);

    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        // the overload that takes a comparer compares differently than the key does
        if (IsLinq(node.Method) &&
            node.Method.Name == nameof(Queryable.Distinct) &&
            node.Arguments.Count == 1)
        {
            var key = UniqueKey(node.Arguments[0]);
            if (key != null)
            {
                throw new($"Distinct() is redundant, since each row includes the key of {key}, so the rows are already unique. Remove it.");
            }
        }

        return base.VisitMethodCall(node);
    }

    // describes the key, for example `Company (Id)`, when the rows of source include it
    string? UniqueKey(Expression source)
    {
        var selected = source;
        LambdaExpression? selector = null;
        if (selected is MethodCallExpression
            {
                Method.Name: nameof(Queryable.Select),
                Arguments.Count: 2
            } select &&
            IsLinq(select.Method))
        {
            selector = select.Arguments[1].Unquote() as LambdaExpression;
            // the overload with an index has a second parameter
            if (selector is not
                {
                    Parameters.Count: 1
                })
            {
                return null;
            }

            selected = select.Arguments[0];
        }

        if (!ReturnsEachEntityOnce(selected))
        {
            return null;
        }

        var element = ElementType(selected.Type);
        var entityType = element == null ? null : model.FindEntityType(element);
        var primaryKey = entityType?.FindPrimaryKey();
        if (primaryKey == null)
        {
            return null;
        }

        var names = primaryKey.Properties.Select(_ => _.Name).ToList();
        if (selector != null &&
            !SelectsKey(selector, names))
        {
            return null;
        }

        return $"{entityType!.DisplayName()} ({string.Join(", ", names)})";
    }

    static bool SelectsKey(LambdaExpression selector, List<string> names)
    {
        var parameter = selector.Parameters[0];
        var body = Unconvert(selector.Body);
        if (body == parameter)
        {
            return true;
        }

        var selected = Values(body)
            .Select(_ => MemberOf(Unconvert(_), parameter))
            .OfType<string>()
            .ToHashSet();
        return names.All(selected.Contains);
    }

    // the values a projection puts in each row
    static IEnumerable<Expression> Values(Expression body)
    {
        if (body is NewExpression @new)
        {
            return @new.Arguments;
        }

        if (body is MemberInitExpression init)
        {
            var expressions = init.Bindings.OfType<MemberAssignment>().Select(_ => _.Expression);
            return init
                .NewExpression
                .Arguments
                .Concat(expressions);
        }

        return [body];
    }

    // the name of the member in `parameter.Member` or `EF.Property(parameter, "Member")`
    static string? MemberOf(Expression expression, ParameterExpression parameter)
    {
        if (expression is MemberExpression
            {
                Expression: not null
            } member &&
            Unconvert(member.Expression) == parameter)
        {
            return member.Member.Name;
        }

        if (expression is not MethodCallExpression
            {
                Method.Name: nameof(EF.Property)
            } call)
        {
            return null;
        }

        var arguments = call.Arguments;
        if (call.Method.DeclaringType == typeof(EF) &&
            Unconvert(arguments[0]) == parameter &&
            arguments[1] is ConstantExpression
            {
                Value: string name
            })
        {
            return name;
        }

        return null;
    }

    bool ReturnsEachEntityOnce(Expression source)
    {
        var current = source;
        while (true)
        {
            switch (current)
            {
                case MethodCallExpression
                {
                    Method.IsStatic: true,
                    Arguments.Count: > 0
                } call when KeepsRows(call.Method):
                    current = call.Arguments[0];
                    continue;
                // raw SQL and temporal queries are derived root types, and can return an entity more than once
                case EntityQueryRootExpression root:
                    return root.GetType() == typeof(EntityQueryRootExpression);
                case MemberExpression member:
                    return IsCollectionNavigation(member.Type);
                default:
                    return false;
            }
        }
    }

    bool IsCollectionNavigation(Type type)
    {
        var element = ElementType(type);
        return element != null &&
               model.FindEntityType(element) != null;
    }

    // operators that return a subset of the rows of their source, each at most once
    static bool KeepsRows(MethodInfo method)
    {
        if (method.DeclaringType == typeof(EntityFrameworkQueryableExtensions) ||
            method.DeclaringType == typeof(RelationalQueryableExtensions))
        {
            return true;
        }

        return IsLinq(method) &&
               method.Name is
                   nameof(Queryable.Where) or
                   nameof(Queryable.OrderBy) or
                   nameof(Queryable.OrderByDescending) or
                   nameof(Queryable.ThenBy) or
                   nameof(Queryable.ThenByDescending) or
                   nameof(Queryable.Take) or
                   nameof(Queryable.Skip) or
                   nameof(Queryable.TakeWhile) or
                   nameof(Queryable.SkipWhile) or
                   nameof(Queryable.Distinct) or
                   nameof(Queryable.OfType);
    }

    static bool IsLinq(MethodInfo method) =>
        method.DeclaringType == typeof(Queryable) ||
        method.DeclaringType == typeof(Enumerable);

    static Type? ElementType(Type type)
    {
        if (type == typeof(string))
        {
            return null;
        }

        return type
            .GetInterfaces()
            .Append(type)
            .FirstOrDefault(_ => _.IsGenericType &&
                                 _.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            ?.GetGenericArguments()[0];
    }

    static Expression Unconvert(Expression expression)
    {
        while (expression is UnaryExpression
               {
                   NodeType:
                   ExpressionType.Convert or
                   ExpressionType.ConvertChecked or
                   ExpressionType.TypeAs
               } unary)
        {
            expression = unary.Operand;
        }

        return expression;
    }
}
