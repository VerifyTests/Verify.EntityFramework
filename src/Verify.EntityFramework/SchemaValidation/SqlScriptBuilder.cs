using System.Collections.Generic;
using System.Linq;
using Microsoft.Data.SqlClient;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;

class SqlScriptBuilder
{
    SchemaSettings settings;

    public SqlScriptBuilder(SchemaSettings settings)
    {
        this.settings = settings;
    }

    public string BuildScript(SqlConnection sqlConnection)
    {
        var builder = new SqlConnectionStringBuilder(sqlConnection.ConnectionString);
        var theServer = new Server(new ServerConnection(sqlConnection));

        var database = theServer.Databases[builder.InitialCatalog];
        return string.Join("\r\n", GetScripts(database));
    }

    IEnumerable<string> GetScripts(Database database)
    {
        foreach (var scriptable in GetScriptingObjects(database))
        {
            if (((dynamic)scriptable).IsSystemObject)
            {
                continue;
            }

            yield return Script(scriptable);
        }
    }

    static string Script(IScriptable scriptable)
    {
        var options = new ScriptingOptions
        {
            ChangeTracking = true,
            NoCollation = true
        };
        return string.Join("\n\n", scriptable.Script(options)
            .Cast<string>()
            .Where(ShouldInclude));
    }

    IEnumerable<IScriptable> GetScriptingObjects(Database database)
    {
        if (settings.Tables)
        {
            for (var index = 0; index < database.Tables.Count; index++)
            {
                var table = database.Tables[index];
                if (settings.IncludeItem(table!.Name))
                {
                    yield return table;
                }
            }
        }

        if (settings.Views)
        {
            for (var index = 0; index < database.Views.Count; index++)
            {
                var view = database.Views[index];
                if (settings.IncludeItem(view!.Name))
                {
                    yield return view;
                }
            }
        }

        if (settings.StoredProcedures)
        {
            for (var index = 0; index < database.StoredProcedures.Count; index++)
            {
                var procedure = database.StoredProcedures[index];
                if (settings.IncludeItem(procedure.Name))
                {
                    yield return procedure;
                }
            }
        }
    }

    static bool ShouldInclude(string script)
    {
        if (script == "SET ANSI_NULLS ON")
        {
            return false;
        }

        if (script == "SET QUOTED_IDENTIFIER ON")
        {
            return false;
        }

        return true;
    }
}