using BinaryExpression = System.Linq.Expressions.BinaryExpression;
using UnaryExpression = System.Linq.Expressions.UnaryExpression;

// A required property, or a required navigation to a principal, is never null, so `_.Name != null` is always true, and
// `_.Name == null` is always false. EF removes a check of a property, or translates it to `0 = 1`, but a check of a
// navigation still joins the principal's table.
// Only a member read from the lambda parameter itself is matched, for example `_.Name`, not `_.Company.Name` or
// `((Manager) _).Title`, and only when the parameter is an element of an entity set, or of a collection navigation,
// followed by operators like Where, OrderBy, and Take. After a left join (DefaultIfEmpty or LeftJoin) the entity itself
// can be null, and a member of a derived type is null for other types.
class RequiredNullCheckDetector(IModel model) :
    ExpressionVisitor
{
    public static void ThrowIfRedundant(Expression query, IModel model) =>
        new RequiredNullCheckDetector(model).Visit(query);

    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        if (node.Method is { IsStatic: true } method &&
            IsLinq(method) &&
            node.Arguments.Count > 1 &&
            HasEntityElements(node.Arguments[0]))
        {
            foreach (var argument in node.Arguments.Skip(1))
            {
                if (argument.Unquote() is LambdaExpression { Parameters: [var parameter] } lambda &&
                    model.FindEntityType(parameter.Type) is { } entityType)
                {
                    new CheckFinder(parameter, entityType).Visit(lambda.Body);
                }
            }
        }

        return base.VisitMethodCall(node);
    }

    // the elements of the source are entities that are never null
    bool HasEntityElements(Expression source)
    {
        while (true)
        {
            switch (source)
            {
                case MethodCallExpression
                {
                    Method.IsStatic: true,
                    Arguments.Count: > 0
                } call when KeepsRows(call.Method):
                    source = call.Arguments[0];
                    continue;
                case EntityQueryRootExpression:
                    return true;
                case MemberExpression member:
                    return IsCollectionNavigation(member.Type);
                default:
                    return false;
            }
        }
    }

    bool IsCollectionNavigation(Type type)
    {
        if (type == typeof(string))
        {
            return false;
        }

        var enumerable = type
            .GetInterfaces()
            .Append(type)
            .FirstOrDefault(_ => _.IsGenericType &&
                                 _.GetGenericTypeDefinition() == typeof(IEnumerable<>));
        return enumerable != null &&
               model.FindEntityType(enumerable.GetGenericArguments()[0]) != null;
    }

    // operators that return a subset of the rows of their source
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
                   nameof(Queryable.AsQueryable);
    }

    static bool IsLinq(MethodInfo method) =>
        method.DeclaringType == typeof(Queryable) ||
        method.DeclaringType == typeof(Enumerable);

    class CheckFinder(ParameterExpression parameter, IEntityType entityType) :
        ExpressionVisitor
    {
        protected override Expression VisitBinary(BinaryExpression node)
        {
            if (node.NodeType is ExpressionType.Equal or ExpressionType.NotEqual)
            {
                var member = Checked(node.Left, node.Right) ?? Checked(node.Right, node.Left);
                if (member != null)
                {
                    Throw(node.NodeType, member);
                }
            }

            return base.VisitBinary(node);
        }

        // the member of the parameter, when it is compared to null
        MemberExpression? Checked(Expression value, Expression other)
        {
            if (Unconvert(other) is not ConstantExpression { Value: null } ||
                Unconvert(value) is not MemberExpression member ||
                member.Expression != parameter)
            {
                return null;
            }

            return member;
        }

        void Throw(ExpressionType type, MemberExpression member)
        {
            var name = member.Member.Name;
            string kind;
            string effect;
            if (entityType.FindProperty(name) is { IsNullable: false })
            {
                kind = "a required property";
                effect = "EF removes the check";
            }
            else if (entityType.FindNavigation(name) is
                     {
                         IsCollection: false,
                         IsOnDependent: true,
                         ForeignKey.IsRequired: true
                     })
            {
                kind = "a required navigation";
                effect = $"EF still joins {name} for the check";
            }
            else
            {
                return;
            }

            if (type == ExpressionType.NotEqual)
            {
                throw new(
                    $"""
                     `{member} != null` is always true, since {name} is {kind}. {effect}.
                     Remove the null check.
                     """);
            }

            throw new(
                $"""
                 `{member} == null` is always false, since {name} is {kind}. EF translates it to `0 = 1`.
                 Remove the null check, or configure {name} as optional if it can be null.
                 """);
        }

        static Expression Unconvert(Expression expression)
        {
            while (expression is UnaryExpression
                   {
                       NodeType: ExpressionType.Convert or ExpressionType.ConvertChecked
                   } unary)
            {
                expression = unary.Operand;
            }

            return expression;
        }
    }
}
