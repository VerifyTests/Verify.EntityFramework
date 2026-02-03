using Microsoft.EntityFrameworkCore.Infrastructure;

class RelationalFactory(
    ShapedQueryCompilingExpressionVisitorDependencies dependencies,
    RelationalShapedQueryCompilingExpressionVisitorDependencies relational) :
    RelationalShapedQueryCompilingExpressionVisitorFactory(dependencies, relational)
{
    public override ShapedQueryCompilingExpressionVisitor Create(QueryCompilationContext context)
    {
        var options = context.ContextOptions;

        var throwForMissingOrderBy = options.FindExtension<MissingOrderByExtension>() != null;
        var expressionRecordingExtension = options.FindExtension<ExpressionRecordingExtension>();

        return new RelationalVisitor(
            Dependencies,
            RelationalDependencies,
            context,
            throwForMissingOrderBy,
            expressionRecordingExtension);
    }
}
