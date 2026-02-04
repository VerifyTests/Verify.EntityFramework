class FormattedScriptGenerator
{
    readonly Sql170ScriptGenerator generator = new(
        new()
        {
            SqlVersion = SqlVersion.Sql170,
            KeywordCasing = KeywordCasing.Lowercase,
            IndentationSize = 2,
            AlignClauseBodies = true
        });

    public string GenerateScript(TSqlFragment fragment)
    {
        generator.GenerateScript(fragment, out var script);

        var collector = new OrderByCollector();
        fragment.Accept(collector);

        foreach (var orderBy in collector.Clauses)
        {
            if (orderBy.OrderByElements.Count <= 1)
            {
                continue;
            }

            var elements = new List<string>(orderBy.OrderByElements.Count);
            foreach (var element in orderBy.OrderByElements)
            {
                generator.GenerateScript(element, out var text);
                elements.Add(text);
            }

            var singleLine = "order by " + string.Join(", ", elements);

            var pos = script.IndexOf(singleLine, StringComparison.Ordinal);
            if (pos == -1)
            {
                continue;
            }

            var lineStart = script.LastIndexOf('\n', pos) + 1;
            var indent = new string(' ', pos - lineStart + "order by ".Length);

            var multiLine = "order by " + string.Join(",\n" + indent, elements);

            script = string.Concat(script.AsSpan(0, pos), multiLine, script.AsSpan(pos + singleLine.Length));
        }

        return script;
    }
}
