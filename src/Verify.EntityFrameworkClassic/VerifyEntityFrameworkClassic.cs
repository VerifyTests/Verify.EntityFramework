namespace VerifyTests;

public static class VerifyEntityFrameworkClassic
{
    public static void Enable()
    {
        VerifierSettings.RegisterFileConverter(
            QueryableToSql,
            (target, _, _) => QueryableConverter.IsQueryable(target));
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