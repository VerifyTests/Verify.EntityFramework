class InPredicateCollector : TSqlFragmentVisitor
{
    public List<InPredicate> Predicates { get; } = [];

    public override void Visit(InPredicate node) =>
        Predicates.Add(node);
}
