sealed class MissingOrderByVisitor : ExpressionVisitor
{
    [return: NotNullIfNotNull("expression")]
    public override Expression? Visit(Expression? expression)
    {
        if (expression is null)
        {
            return null;
        }

        switch (expression)
        {
            case ShapedQueryExpression shapedQueryExpression:
                Visit(shapedQueryExpression.QueryExpression);
                return shapedQueryExpression;

            case RelationalSplitCollectionShaperExpression splitExpression:
                foreach (var table in splitExpression.SelectExpression.Tables)
                {
                    Visit(table);
                }

                Visit(splitExpression.InnerShaper);

                return splitExpression;

            case SelectExpression { Orderings.Count: 0 } selectExpression:
            {
                throw new(
                    $"""
                     SelectExpression must have at least one ordering.
                     Expression:
                     {PrintShortSql(selectExpression)}
                     """);
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