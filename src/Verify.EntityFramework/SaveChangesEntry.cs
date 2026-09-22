// Values are copied, since by the time the recording is verified, SaveChanges has accepted the changes
class SaveChangesEntry
{
    public SaveChangesEntry(string type, IEnumerable<EntityEntry> entries)
    {
        Type = type;
        foreach (var entry in entries)
        {
            var name = entry.Metadata.DisplayName();
            switch (entry.State)
            {
                case EntityState.Added:
                    Added.Add(new(name, AddedMembers(entry)));
                    break;
                case EntityState.Modified:
                    Modified.Add(new(name, [.. IdMembers(entry), .. ModifiedMembers(entry)]));
                    break;
                case EntityState.Deleted:
                    Deleted.Add(new(name, IdMembers(entry)));
                    break;
            }
        }
    }

    public string Type { get; }
    public List<SavedEntity> Added { get; } = [];
    public List<SavedEntity> Modified { get; } = [];
    public List<SavedEntity> Deleted { get; } = [];

    static List<(string name, object? value)> AddedMembers(EntityEntry entry) =>
        entry.Properties
            .Select(_ => (_.Metadata.Name, _.CurrentValue))
            .ToList();

    static IEnumerable<(string name, object? value)> ModifiedMembers(EntityEntry entry) =>
        entry.ChangedProperties()
            .Select(_ => (
                _.Metadata.Name,
                (object?) new
                {
                    Original = _.OriginalValue,
                    Current = _.CurrentValue
                }));

    static List<(string name, object? value)> IdMembers(EntityEntry entry)
    {
        var ids = entry
            .FindPrimaryKeyValues()
            .ToList();
        if (ids.Count > 1)
        {
            return [("Ids", ids)];
        }

        return ids;
    }
}

record SavedEntity(string Name, List<(string name, object? value)> Members);
