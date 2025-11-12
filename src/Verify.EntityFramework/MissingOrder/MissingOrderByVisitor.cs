sealed class MissingOrderByVisitor :
    ExpressionVisitor
{
    List<TableExpressionBase> orderedTables = [];

    public override Expression? Visit(Expression? expression)
    {
        if (expression is null)
        {
            return null;
        }

        switch (expression)
        {
            case ShapedQueryExpression shaped:
                if(shaped.ResultCardinality != ResultCardinality.Enumerable)
                {
                    return null;
                }
                Visit(shaped.QueryExpression);
                return shaped;

            case RelationalSplitCollectionShaperExpression split:
                foreach (var table in split.SelectExpression.Tables)
                {
                    Visit(table);
                }

                Visit(split.InnerShaper);

                return split;

            case TableExpression tableExpression:
            {
                foreach (var table in orderedTables)
                {
                    if (table == tableExpression)
                    {
                        return base.Visit(expression);
                    }

                    if (table is PredicateJoinExpressionBase join)
                    {
                        if (join.Table == tableExpression)
                        {
                            return base.Visit(expression);
                        }
                    }
                }

                throw new(
                    $"""
                     TableExpression must have at least one ordering.
                     Expression:
                     {ExpressionPrinter.Print(tableExpression)}
                     """);
            }
            case SelectExpression select:
            {
                var orderings = select.Orderings;
                if (orderings.Count == 0)
                {
                    throw new(
                        $"""
                         SelectExpression must have at least one ordering.
                         Expression:
                         {PrintShortSql(select)}
                         """);
                }

                foreach (var ordering in orderings)
                {
                    if (ordering.Expression is not ColumnExpression column)
                    {
                        continue;
                    }

                    if (!TryFindTable(select.Tables, column.TableAlias, out var table))
                    {
                        continue;
                    }

                    orderedTables.Add(table);
                }

                return base.Visit(expression);
            }

            default:
                return base.Visit(expression);
        }
    }

    static bool TryFindTable(IReadOnlyList<TableExpressionBase> tables, string name, [NotNullWhen(true)] out TableExpressionBase? result)
    {
        foreach (var table in tables)
        {
            if (table.NameOrAlias() == name)
            {
                result = table;
                return true;
            }

            if (table is LeftJoinExpression join &&
                join.Table.NameOrAlias() == name)
            {
                result = join;
                return true;
            }
        }

        result = null;
        return false;
    }

    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "PrintShortSql")]
    static extern string PrintShortSql(SelectExpression expression);
}