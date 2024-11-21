class RelationalFactory(
    ShapedQueryCompilingExpressionVisitorDependencies dependencies,
    RelationalShapedQueryCompilingExpressionVisitorDependencies relational) :
    RelationalShapedQueryCompilingExpressionVisitorFactory(dependencies, relational)
{
    public override ShapedQueryCompilingExpressionVisitor Create(QueryCompilationContext context)
        => new RelationalVisitor(
            Dependencies,
            RelationalDependencies,
            context);
}