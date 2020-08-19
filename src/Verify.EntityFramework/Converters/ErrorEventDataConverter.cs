using System;
using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Newtonsoft.Json;
using VerifyTests;

class ErrorEventDataConverter :
    WriteOnlyJsonConverter<CommandErrorEventData>
{
    public override void WriteJson(JsonWriter writer, CommandErrorEventData? data, JsonSerializer serializer)
    {
        if (data == null)
        {
            return;
        }

        var command = data.Command;
        serializer.Serialize(writer, new Wrapper(command.CommandText, command.Parameters, data.Exception));
    }

    class Wrapper
    {
        public string Type { get => "Error"; }
        public string Command { get; }
        public DbParameterCollection? Parameters { get; }
        public Exception Exception { get; }

        public Wrapper(string command, DbParameterCollection parameters, Exception exception)
        {
            Command = command;
            Exception = exception;
            if (parameters.Count > 0)
            {
                Parameters = parameters;
            }
        }
    }
}