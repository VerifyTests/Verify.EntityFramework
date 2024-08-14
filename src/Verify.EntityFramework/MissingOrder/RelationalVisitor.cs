class RelationalVisitor :
    RelationalShapedQueryCompilingExpressionVisitor
{
    public RelationalVisitor(ShapedQueryCompilingExpressionVisitorDependencies dependencies, RelationalShapedQueryCompilingExpressionVisitorDependencies relationalDependencies, QueryCompilationContext context) :
        base(dependencies, relationalDependencies, context)
    {
    }

    [return: NotNullIfNotNull("node")]
    public override Expression? Visit(Expression? node)
    {
        new MissingOrderByVisitor().Visit(node);
        return base.Visit(node);
    }
}