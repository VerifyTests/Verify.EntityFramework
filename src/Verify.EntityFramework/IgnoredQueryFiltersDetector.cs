// IgnoreQueryFilters() does nothing when no entity type in the query has a query filter. With filter keys, it does
// nothing when none of the entity types has a filter with one of those keys.
// The entity types in a query are those of its roots, of any expression in it, for example a navigation in a
// projection, of the join types of their skip navigations, and of their AutoInclude navigations. The filters of an entity
// type are declared on the root of its hierarchy.
// The check leans towards not throwing: a string Include, or filter keys that are not a constant, skip the check.
static class IgnoredQueryFiltersDetector
{
    public static void ThrowIfIgnored(Expression query, IModel model)
    {
        MethodCallExpression? ignore = null;
        var ignoresAutoIncludes = false;
        var expression = query;
        while (expression is MethodCallExpression { Method.IsStatic: true, Arguments.Count: > 0 } call)
        {
            var method = call.Method;
            if (method.DeclaringType == typeof(EntityFrameworkQueryableExtensions))
            {
                switch (method.Name)
                {
                    case nameof(EntityFrameworkQueryableExtensions.IgnoreQueryFilters):
                        ignore ??= call;
                        break;
                    case nameof(EntityFrameworkQueryableExtensions.IgnoreAutoIncludes):
                        ignoresAutoIncludes = true;
                        break;
                    // a string Include can not be checked without resolving the path
                    case nameof(EntityFrameworkQueryableExtensions.Include) when call.Arguments[1].Unquote() is not LambdaExpression:
                        return;
                }
            }

            expression = call.Arguments[0];
        }

        if (ignore == null)
        {
            return;
        }

        HashSet<string>? keys = null;
        if (ignore.Arguments.Count > 1)
        {
            if (ignore.Arguments[1] is not ConstantExpression { Value: IEnumerable<string> values })
            {
                return;
            }

            keys = values.ToHashSet();
        }

        var filters = EntityTypes(query, model, ignoresAutoIncludes)
            .Select(_ => _.GetRootType())
            .Distinct()
            .SelectMany(_ => _.GetDeclaredQueryFilters());
        if (keys == null)
        {
            if (filters.Any())
            {
                return;
            }

            throw new(
                $"""
                 {ignore.Describe()} is ignored, since no entity type in the query has a query filter.
                 Remove it.
                 """);
        }

        if (filters.Any(_ => _.Key != null && keys.Contains(_.Key)))
        {
            return;
        }

        throw new(
            $"""
             {ignore.Describe()} is ignored, since no entity type in the query has a query filter with the key {string.Join(" or ", keys)}.
             Remove it, or use the key of the filter to ignore.
             """);
    }

    static HashSet<IEntityType> EntityTypes(Expression query, IModel model, bool ignoresAutoIncludes)
    {
        var finder = new EntityTypeFinder(model);
        finder.Visit(query);
        var found = finder.Found;

        // the types that other types bring in, repeated until no new type is found
        var pending = new Queue<IEntityType>(found);
        while (pending.TryDequeue(out var entityType))
        {
            var brought = entityType
                .GetSkipNavigations()
                .Select(_ => _.JoinEntityType);
            if (!ignoresAutoIncludes)
            {
                brought = brought.Concat(
                    entityType
                        .GetNavigations()
                        .Cast<INavigationBase>()
                        .Concat(entityType.GetSkipNavigations())
                        .Where(_ => _.IsEagerLoaded)
                        .Select(_ => _.TargetEntityType));
            }

            foreach (var type in brought)
            {
                if (found.Add(type))
                {
                    pending.Enqueue(type);
                }
            }
        }

        return found;
    }

    class EntityTypeFinder(IModel model) :
        ExpressionVisitor
    {
        public HashSet<IEntityType> Found { get; } = [];

        public override Expression? Visit(Expression? node)
        {
            if (node == null)
            {
                return node;
            }

            if (node is EntityQueryRootExpression root)
            {
                Found.Add(root.EntityType);
            }

            Add(node.Type);
            return base.Visit(node);
        }

        // a type, or the element type of a collection, that is mapped to one or more entity types
        void Add(Type type)
        {
            if (type == typeof(string))
            {
                return;
            }

            var element = type
                .GetInterfaces()
                .Append(type)
                .FirstOrDefault(_ => _.IsGenericType &&
                                     _.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                ?.GetGenericArguments()[0];
            foreach (var entityType in model.GetEntityTypes())
            {
                if (entityType.ClrType == type ||
                    entityType.ClrType == element)
                {
                    Found.Add(entityType);
                }
            }
        }
    }
}
