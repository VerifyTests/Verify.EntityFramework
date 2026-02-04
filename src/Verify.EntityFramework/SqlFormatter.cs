static class SqlFormatter
{
    static readonly FormattedScriptGenerator generator = new();

    public static StringBuilder Format(string input)
    {
        var parser = new TSql170Parser(false);
        using var reader = new StringReader(input);
        var fragment = parser.Parse(reader, out var errors);

        if (errors.Count > 0)
        {
            throw new(
                $"""
                 Failed to parse sql.

                 Errors:
                 {string.Join(Environment.NewLine, errors.Select(_ => _.Message))}

                 Sql input:
                 {input}
                 """);
        }

        var visitor = new RemoveSquareBracketVisitor();
        fragment.Accept(visitor);

        var script = generator.GenerateScript(fragment);

        var builder = new StringBuilder(script);
        builder.TrimEnd();
        if (builder[^1] == ';')
        {
            builder.Length--;
        }

        return builder;
    }
}
