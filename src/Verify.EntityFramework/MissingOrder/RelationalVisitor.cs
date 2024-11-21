class RelationalVisitor(
    ShapedQueryCompilingExpressionVisitorDependencies dependencies,
    RelationalShapedQueryCompilingExpressionVisitorDependencies relational,
    QueryCompilationContext context) :
        RelationalShapedQueryCompilingExpressionVisitor(dependencies, relational, context)
{
    [return: NotNullIfNotNull(nameof(node))]
    public override Expression? Visit(Expression? node)
    {
        new MissingOrderByVisitor().Visit(node);
        return base.Visit(node);
    }
}