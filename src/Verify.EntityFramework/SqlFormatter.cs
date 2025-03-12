using System.Globalization;

static class SqlFormatter
{
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

        var generator = new Sql170ScriptGenerator(
            new()
            {
                SqlVersion = SqlVersion.Sql170,
                KeywordCasing = KeywordCasing.Lowercase,
                IndentationSize = 2,
                AlignClauseBodies = true
            });

        var builder = new StringBuilder();
        using (var writer = new StringWriter(builder, CultureInfo.InvariantCulture))
        {
            generator.GenerateScript(fragment, writer);
        }

        builder.TrimEnd();
        if (builder[^1] == ';')
        {
            builder.Length--;
        }

        return builder;
    }
}