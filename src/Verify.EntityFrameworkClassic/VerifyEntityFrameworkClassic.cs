namespace VerifyTests;

public static class VerifyEntityFrameworkClassic
{
    public static bool Initialized { get; private set; }

    public static void Initialize()
    {
        if (Initialized)
        {
            throw new("Already Initialized");
        }

        Initialized = true;

        VerifierSettings.RegisterFileConverter(
            QueryableToSql,
            (target, _) => QueryableConverter.IsQueryable(target));
        VerifierSettings.AddExtraSettings(serializer =>
        {
            var converters = serializer.Converters;
            converters.Add(new TrackerConverter());
            converters.Add(new QueryableConverter());
        });
    }

    static ConversionResult QueryableToSql(object arg, IReadOnlyDictionary<string, object> context)
    {
        var sql = QueryableConverter.QueryToSql(arg);
        return new(null, "txt", sql);
    }
}