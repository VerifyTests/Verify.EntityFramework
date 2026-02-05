#pragma warning disable EF9002

class DescriptiveSqlAliasManager : SqlAliasManager
{
    Dictionary<string, int> aliases = new(StringComparer.OrdinalIgnoreCase);

    public override string GenerateTableAlias(string name)
    {
        if (aliases.TryGetValue(name, out var counter))
        {
            aliases[name] = counter + 1;
            return name + counter;
        }

        aliases[name] = 0;
        return name;
    }

    protected override Dictionary<string, string>? RemapTableAliases(IReadOnlySet<string> usedAliases) =>
        // Skip gap-closing since the base implementation assumes single-char alias prefixes
        null;
}
