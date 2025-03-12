class RemoveSquareBracketVisitor : TSqlFragmentVisitor
{
    public override void Visit(Identifier node)
    {
        if (node.QuoteType == QuoteType.SquareBracket)
        {
            node.QuoteType = QuoteType.NotQuoted;
        }

        base.Visit(node);
    }
}