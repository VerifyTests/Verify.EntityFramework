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

        var visitor = new RemoveSquareBracketVisitor();
        fragment.Accept(visitor);

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

        FormatOrderBy(builder);

        return builder;
    }

    static void FormatOrderBy(StringBuilder builder)
    {
        var text = builder.ToString();
        builder.Clear();

        using var lineReader = new StringReader(text);
        string? line;
        var first = true;

        while ((line = lineReader.ReadLine()) != null)
        {
            if (!first)
            {
                builder.Append('\n');
            }

            first = false;

            var trimmed = line.TrimStart();
            if (trimmed.StartsWith("order by ") && trimmed.Contains(','))
            {
                var leadingSpaces = line.Length - trimmed.Length;
                var prefix = new string(' ', leadingSpaces) + "order by ";
                var columns = trimmed["order by ".Length..];
                var items = SplitRespectingParens(columns);

                if (items.Count > 1)
                {
                    var pad = new string(' ', prefix.Length);
                    for (var j = 0; j < items.Count; j++)
                    {
                        if (j > 0)
                        {
                            builder.Append('\n');
                            builder.Append(pad);
                        }
                        else
                        {
                            builder.Append(prefix);
                        }

                        builder.Append(items[j].Trim());
                        if (j < items.Count - 1)
                        {
                            builder.Append(',');
                        }
                    }

                    continue;
                }
            }

            builder.Append(line);
        }
    }

    static List<string> SplitRespectingParens(string input)
    {
        var items = new List<string>();
        var depth = 0;
        var start = 0;

        for (var i = 0; i < input.Length; i++)
        {
            switch (input[i])
            {
                case '(':
                    depth++;
                    break;
                case ')':
                    depth--;
                    break;
                case ',' when depth == 0:
                    items.Add(input[start..i]);
                    start = i + 1;
                    break;
            }
        }

        items.Add(input[start..]);
        return items;
    }
}