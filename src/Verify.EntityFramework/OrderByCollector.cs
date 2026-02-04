class OrderByCollector : TSqlFragmentVisitor
{
    public List<OrderByClause> Clauses { get; } = [];

    public override void Visit(OrderByClause node) =>
        Clauses.Add(node);
}
