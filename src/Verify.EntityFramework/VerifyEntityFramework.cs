using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Verify.EntityFramework
{
    public static class VerifyEntityFramework
    {
        public static void Enable()
        {
            SharedVerifySettings.RegisterFileConverter("txt", QueryableToSql, QueryableConverter.IsQueryable);
            SharedVerifySettings.ModifySerialization(settings =>
            {
                settings.AddExtraSettings(serializer =>
                {
                    var converters = serializer.Converters;
                    converters.Add(new DbContextConverter());
                    converters.Add(new QueryableConverter());
                });
            });
        }

        static IEnumerable<Stream> QueryableToSql(object arg)
        {
            var sql = QueryableConverter.QueryToSql(arg);
            yield return StringToMemoryStream(sql);
        }

        static MemoryStream StringToMemoryStream(string text)
        {
            var bytes = Encoding.UTF8.GetBytes(text.Replace("\r\n", "\n"));
            return new MemoryStream(bytes);
        }
    }
}