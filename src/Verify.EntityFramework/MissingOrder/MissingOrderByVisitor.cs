sealed class MissingOrderByVisitor : ExpressionVisitor
{
    List<OrderingExpression> orderedExpressions = [];

    public override Expression? Visit(Expression? expression)
    {
        if (expression is null)
        {
            return null;
        }

        switch (expression)
        {
            case ShapedQueryExpression shapedQueryExpression:
                if(shapedQueryExpression.ResultCardinality != ResultCardinality.Enumerable)
                {
                    return null;
                }
                Visit(shapedQueryExpression.QueryExpression);
                return shapedQueryExpression;

            case RelationalSplitCollectionShaperExpression splitExpression:
                foreach (var table in splitExpression.SelectExpression.Tables)
                {
                    Visit(table);
                }

                Visit(splitExpression.InnerShaper);

                return splitExpression;

            case TableExpression tableExpression:
            {
                foreach (var orderedExpression in orderedExpressions)
                {
                    if (orderedExpression.Expression is ColumnExpression columnExpression)
                    {
                        Debug.WriteLine(columnExpression);
                        // if (columnExpression..Table == tableExpression.Alias)
                        // {
                        //     return base.Visit(expression);
                        // }
                        //
                        // if (columnExpression.Table is PredicateJoinExpressionBase joinExpression)
                        // {
                        //     if (joinExpression.Table == tableExpression)
                        //     {
                        //         return base.Visit(expression);
                        //     }
                        // }
                    }
                }

                throw new(
                    $"""
                     TableExpression must have at least one ordering.
                     Expression:
                     {ExpressionPrinter.Print(tableExpression)}
                     """);
            }
            case SelectExpression selectExpression:
            {
                var orderings = selectExpression.Orderings;
                if (orderings.Count == 0)
                {
                    throw new(
                        $"""
                         SelectExpression must have at least one ordering.
                         Expression:
                         {PrintShortSql(selectExpression)}
                         """);
                }

                foreach (var ordering in orderings)
                {
                    orderedExpressions.Add(ordering);
                }

                return base.Visit(expression);
            }

            case NonQueryExpression nonQueryExpression:
                return nonQueryExpression;

            default:
                return base.Visit(expression);
        }
    }

    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "PrintShortSql")]
    static extern string PrintShortSql(SelectExpression expression);
}