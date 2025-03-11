static class SqlFormatter
{
    public static ReadOnlySpan<char> Format(string input)
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

        var generator = new Sql170ScriptGenerator(
            new()
            {
                SqlVersion = SqlVersion.Sql170,
                KeywordCasing = KeywordCasing.Lowercase,
                IndentationSize = 2,
                AlignClauseBodies = true,
            });

        generator.GenerateScript(fragment, out var output);
        return output.AsSpan().TrimEnd().TrimEnd(';');
    }
}