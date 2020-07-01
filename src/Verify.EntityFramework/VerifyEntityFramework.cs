using System.IO;
using System.Text;

namespace VerifyTests
{
    public static class VerifyEntityFramework
    {
        public static void Enable()
        {
            VerifierSettings.RegisterFileConverter(
                QueryableToSql,
                (target, settings) => QueryableConverter.IsQueryable(target));
            VerifierSettings.ModifySerialization(settings =>
            {
                settings.AddExtraSettings(serializer =>
                {
                    var converters = serializer.Converters;
                    converters.Add(new DbContextConverter());
                    converters.Add(new QueryableConverter());
                });
            });
        }

        static ConversionResult QueryableToSql(object arg, VerifySettings settings)
        {
            var sql = QueryableConverter.QueryToSql(arg);
            return new ConversionResult(
                null,
                new[]
                {
                    new ConversionStream("txt", StringToMemoryStream(sql)),
                });
        }

        static MemoryStream StringToMemoryStream(string text)
        {
            var bytes = Encoding.UTF8.GetBytes(text.Replace("\r\n", "\n"));
            return new MemoryStream(bytes);
        }
    }
}