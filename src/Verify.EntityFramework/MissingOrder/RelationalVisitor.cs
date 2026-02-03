class RelationalVisitor(
    ShapedQueryCompilingExpressionVisitorDependencies dependencies,
    RelationalShapedQueryCompilingExpressionVisitorDependencies relational,
    QueryCompilationContext context,
    bool throwForMissingOrderBy,
    ExpressionRecordingExtension? expressionRecordingExtension) :
        RelationalShapedQueryCompilingExpressionVisitor(dependencies, relational, context)
{
    [return: NotNullIfNotNull(nameof(node))]
    public override Expression? Visit(Expression? node)
    {
        if (throwForMissingOrderBy)
        {
            new MissingOrderByVisitor().Visit(node);
        }

        if (expressionRecordingExtension != null && node != null)
        {
            RecordExpression(node);
        }

        return base.Visit(node);
    }

    void RecordExpression(Expression node)
    {
        var expressionText = ExpressionPrinter.Print(node);
        var entry = new ExpressionEntry("QueryCompilation", expressionText);

        var identifier = expressionRecordingExtension!.Identifier;
        if (identifier is null)
        {
            Recording.TryAdd("ef-expression", entry);
        }
        else
        {
            Recording.TryAdd(identifier, "ef-expression", entry);
        }
    }
}
