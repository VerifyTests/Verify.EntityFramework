using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Internal;
using Newtonsoft.Json;
using VerifyTests;

class ExecutedEventDataConverter :
    WriteOnlyJsonConverter<CommandExecutedEventData>
{
    public override void WriteJson(JsonWriter writer, CommandExecutedEventData? data, JsonSerializer serializer)
    {
        if (data == null)
        {
            return;
        }

        var command = data.Command;
        serializer.Serialize(writer, new Wrapper(command.CommandText, command.Parameters));
    }

    class Wrapper
    {
        public string Type { get => "Execute"; }
        public string Command { get; }
        public DbParameterCollection? Parameters { get; }

        public Wrapper(string command, DbParameterCollection parameters)
        {
            Command = command;
            if (parameters.Any())
            {
                Parameters = parameters;
            }
        }
    }
}