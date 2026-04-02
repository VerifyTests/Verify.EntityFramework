class DescriptiveParameterNameGenerator :
    ParameterNameGenerator
{
    Dictionary<string, int> names = new(StringComparer.OrdinalIgnoreCase);
    string? pendingColumnName;

    public void SetColumnHint(string columnName) => pendingColumnName = columnName;

    public override string GenerateNext()
    {
        var hint = pendingColumnName;
        pendingColumnName = null;

        if (hint == null)
        {
            return base.GenerateNext();
        }

        // Keep the base counter in sync
        base.GenerateNext();

        if (names.TryGetValue(hint, out var counter))
        {
            names[hint] = counter + 1;
            return hint + counter;
        }

        names[hint] = 1;
        return hint;
    }

    public override void Reset()
    {
        base.Reset();
        names.Clear();
        pendingColumnName = null;
    }
}
