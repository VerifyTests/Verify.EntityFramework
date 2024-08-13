
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using Microsoft.EntityFrameworkCore.SqlServer.Query.Internal;
using Microsoft.EntityFrameworkCore.Storage;

class MissingOrderByVisitorInterceptor : IQueryExpressionInterceptor
{
    public Expression QueryCompilationStarting(Expression queryExpression, QueryExpressionEventData eventData)
        => new KeyOrderingExpressionVisitor().Visit(queryExpression);

    class KeyOrderingExpressionVisitor : ExpressionVisitor
    {
        protected override Expression VisitMethodCall(MethodCallExpression? methodCallExpression)
        {
            var methodInfo = methodCallExpression!.Method;
            if (methodInfo.DeclaringType == typeof(Queryable)
                && methodInfo.Name == nameof(Queryable.OrderBy)
                && methodInfo.GetParameters().Length == 2)
            {
            }

            return base.VisitMethodCall(methodCallExpression);
        }

        protected override Expression VisitExtension(Expression extensionExpression)
        {
            return base.VisitExtension(extensionExpression);
        }


        [return: NotNullIfNotNull("node")]
        public override Expression? Visit(Expression? node)
        {
            var expression = base.Visit(node);
            if (expression != null)
            {
                var type = expression.GetType();
            Debug.WriteLine(type);
            }

            return expression;
        }
    }

}


sealed class MissingOrderByVisitor : ExpressionVisitor
{
    [return: NotNullIfNotNull("expression")]
    public override Expression? Visit(Expression? expression)
    {
        if (expression is null)
        {
            return null;
        }

        var type = expression.GetType();

        var propertyInfo = type.GetProperty("Orderings", BindingFlags.Instance | BindingFlags.Public);
        if (propertyInfo != null)
        {
            Debug.WriteLine(propertyInfo);
        }
        switch (expression)
        {
            case ShapedQueryExpression shapedQueryExpression:
                Visit(shapedQueryExpression.QueryExpression);
                return shapedQueryExpression;

            case RelationalSplitCollectionShaperExpression relationalSplitCollectionShaperExpression:
                foreach (var table in relationalSplitCollectionShaperExpression.SelectExpression.Tables)
                {
                    Visit(table);
                }

                Visit(relationalSplitCollectionShaperExpression.InnerShaper);

                return relationalSplitCollectionShaperExpression;

            case SelectExpression { Orderings.Count: 0 }:
            {
                throw new InvalidOperationException("SplitQueryOffsetWithoutOrderBy");
            }

            case NonQueryExpression nonQueryExpression:
                return nonQueryExpression;

            default:
                return base.Visit(expression);
        }
    }
}

public class Foo(QueryTranslationPostprocessorDependencies dependencies, RelationalQueryTranslationPostprocessorDependencies relationalDependencies, IRelationalTypeMappingSource typeMappingSource)
    : SqlServerQueryTranslationPostprocessorFactory(dependencies, relationalDependencies, typeMappingSource)
{
    public override QueryTranslationPostprocessor Create(QueryCompilationContext queryCompilationContext) =>
        new Bar(dependencies, relationalDependencies, queryCompilationContext, typeMappingSource);
}

public class Bar : SqlServerQueryTranslationPostprocessor
{
    public Bar(QueryTranslationPostprocessorDependencies dependencies, RelationalQueryTranslationPostprocessorDependencies relationalDependencies, QueryCompilationContext queryCompilationContext, IRelationalTypeMappingSource typeMappingSource) : base(dependencies, relationalDependencies, queryCompilationContext, typeMappingSource)
    {
    }

    public override Expression Process(Expression query)
    {
        var expression = base.Process(query);

        new MissingOrderByVisitor().Visit(expression);

        return expression;
    }
}