using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;

static class LogEntryReader
{
    static FieldInfo optionsField;

    static LogEntryReader()
    {
        optionsField = typeof(DbContext).GetField("_options", BindingFlags.Instance | BindingFlags.NonPublic);
        if (optionsField == null)
        {
            throw new Exception("Could not find `_options` field.");
        }
    }

    public static bool TryReadLogEntries(this DbContext data, out List<CommandEventData>? events)
    {
        var options = (DbContextOptions) optionsField.GetValue(data)!;

        var extension = options.Extensions
            .OfType<CoreOptionsExtension>()
            .SingleOrDefault();
        var interceptor = extension?.Interceptors?
            .OfType<LogCommandInterceptor>()
            .SingleOrDefault();
        if (interceptor != null)
        {
            var instanceId = data.ContextId.InstanceId;
            return interceptor.Entries.TryRemove(instanceId, out events);
        }

        events = null;
        return false;
    }
}