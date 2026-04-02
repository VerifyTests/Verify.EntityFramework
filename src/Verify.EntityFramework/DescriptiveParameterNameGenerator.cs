class DescriptiveParameterNameGenerator :
    ParameterNameGenerator
{
    // columnName → (count, firstEntityName)
    Dictionary<string, (int count, string entityName)> names = new(StringComparer.OrdinalIgnoreCase);

    // All generated param names, for collision detection
    HashSet<string> allGenerated = new(StringComparer.OrdinalIgnoreCase);

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

        if (names.TryGetValue(col, out var info))
        {
            // Collision on column name - try entity-prefixed name
            var prefixed = entity + col;

            if (!allGenerated.Contains(prefixed))
            {
                names[col] = (info.count + 1, info.entityName);
                allGenerated.Add(prefixed);
                return prefixed;
            }

            // Entity-prefixed name also collides, fall back to counter
            names[col] = (info.count + 1, info.entityName);
            var fallback = col + info.count;
            allGenerated.Add(fallback);
            return fallback;
        }

        // First occurrence of this column name
        if (!allGenerated.Contains(col))
        {
            names[col] = (1, entity!);
            allGenerated.Add(col);
            return col;
        }

        // Column name collides with a previously generated name (e.g. from an entity-prefix)
        var entityPrefixed = entity + col;
        if (!allGenerated.Contains(entityPrefixed))
        {
            names[col] = (1, entity!);
            allGenerated.Add(entityPrefixed);
            return entityPrefixed;
        }

        // Both collide, use counter
        names[col] = (1, entity!);
        var numbered = col + "1";
        allGenerated.Add(numbered);
        return numbered;
    }

    public override void Reset()
    {
        base.Reset();
        names.Clear();
        allGenerated.Clear();
        pendingColumnName = null;
        pendingEntityName = null;
    }
}
