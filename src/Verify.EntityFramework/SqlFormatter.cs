static class SqlFormatter
{
    static readonly FormattedScriptGenerator generator = new();

    // Appended by EF ToQueryString when a query executes in split-query mode. It is not sql.
    const string splitQueryNote = "This LINQ query is being executed in split-query mode";

    public static StringBuilder Format(string input)
    {
        var noteIndex = input.IndexOf(splitQueryNote, StringComparison.Ordinal);
        if (noteIndex < 0)
        {
            return FormatSql(input);
        }

        var note = input[noteIndex..].Trim();
        var builder = FormatSql(input[..noteIndex]);
        builder.AppendLine();
        builder.AppendLine();
        builder.Append("-- ");
        builder.Append(note);
        return builder;
    }

    static StringBuilder FormatSql(string input)
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
