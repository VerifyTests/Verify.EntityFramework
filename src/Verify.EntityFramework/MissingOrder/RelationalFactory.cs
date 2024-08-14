class RelationalFactory : RelationalShapedQueryCompilingExpressionVisitorFactory
{
    public RelationalFactory(ShapedQueryCompilingExpressionVisitorDependencies dependencies, RelationalShapedQueryCompilingExpressionVisitorDependencies relationalDependencies) :
        base(dependencies, relationalDependencies)
    {
    }

    public override  ShapedQueryCompilingExpressionVisitor Create(QueryCompilationContext context)
        => new RelationalVisitor(
            Dependencies,
            RelationalDependencies,
            context);
}