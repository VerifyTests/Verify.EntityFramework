using System;
using System.Collections.Generic;
using System.Data.Common;
using System.IO;
using System.Text;
using Microsoft.Data.SqlClient;

namespace Verify.EntityFramework
{
    public static class VerifyEntityFramework
    {
        public static void Enable()
        {
            SharedVerifySettings.RegisterFileConverter<DbConnection>("sql", ConnectionToSql);
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

        static IEnumerable<Stream> ConnectionToSql(DbConnection dbConnection, VerifySettings settings)
        {
            if (!(dbConnection is SqlConnection sqlConnection))
            {
                throw new Exception("Only verification of a SqlConnection is supported");
            }

            var schemaSettings = settings.GetSchemaSettings();
            var builder = new SqlScriptBuilder(schemaSettings);
            var sql = builder.BuildScript(sqlConnection);
            yield return StringToMemoryStream(sql);
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