#pragma warning disable EF9002

class DescriptiveSqlAliasManager : SqlAliasManager
{
    Dictionary<string, int> aliases = new(StringComparer.OrdinalIgnoreCase);

    public override string GenerateTableAlias(string name)
    {
        var lowerName = name.ToLowerInvariant();

        if (aliases.TryGetValue(lowerName, out var counter))
        {
            aliases[lowerName] = counter + 1;
            return lowerName + counter;
        }

        aliases[lowerName] = 0;
        return lowerName;
    }

    protected override Dictionary<string, string>? RemapTableAliases(IReadOnlySet<string> usedAliases) =>
        // Skip gap-closing since the base implementation assumes single-char alias prefixes
        null;
}
