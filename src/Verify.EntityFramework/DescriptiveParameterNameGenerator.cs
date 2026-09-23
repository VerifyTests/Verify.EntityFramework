class DescriptiveParameterNameGenerator :
    ParameterNameGenerator
{
    // All generated param names, for collision detection
    HashSet<string> allGenerated = [with(StringComparer.OrdinalIgnoreCase)];

    string? pendingColumnName;
    string? pendingEntityName;

    public void SetColumnHint(string entityName, string columnName)
    {
        pendingEntityName = entityName;
        pendingColumnName = columnName;
    }

    public override string GenerateNext()
    {
        var col = pendingColumnName;
        var entity = pendingEntityName;
        pendingColumnName = null;
        pendingEntityName = null;

        if (col == null)
        {
            return base.GenerateNext();
        }

        // Keep the base counter in sync
        base.GenerateNext();

        var name = Sanitize(col);
        if (allGenerated.Add(name))
        {
            return name;
        }

        // Collision on column name - try entity-prefixed name
        var prefixed = Sanitize(entity + col);
        if (allGenerated.Add(prefixed))
        {
            return prefixed;
        }

        // Both collide, use the first free counter
        for (var counter = 1; ; counter++)
        {
            var numbered = name + counter;
            if (allGenerated.Add(numbered))
            {
                return numbered;
            }
        }
    }

    // Column and entity names can contain characters that are not valid in a parameter name,
    // for example spaces, or the backtick in Dictionary`2 of a shared-type join entity
    static string Sanitize(string name)
    {
        var builder = new StringBuilder(name.Length + 1);
        foreach (var ch in name)
        {
            if (char.IsLetterOrDigit(ch) || ch == '_')
            {
                builder.Append(ch);
            }
        }

        if (builder.Length == 0 ||
            char.IsDigit(builder[0]))
        {
            builder.Insert(0, '_');
        }

        return builder.ToString();
    }

    public override void Reset()
    {
        base.Reset();
        allGenerated.Clear();
        pendingColumnName = null;
        pendingEntityName = null;
    }
}
