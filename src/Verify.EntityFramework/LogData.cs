namespace VerifyTests.EntityFramework;

public class LogEntry
{
    public string Type { get; }
    public DateTimeOffset StartTime { get; }
    internal bool IsSqlServer { get; }
    public TimeSpan Duration { get; }
    public bool HasTransaction { get; }
    public Exception? Exception { get; }
    public IDictionary<string, object> Parameters { get; }
    public string Text { get; }

    public LogEntry(
        string type,
        DbCommand command,
        CommandEndEventData data,
        Exception? exception = null)
    {
        Type = type;
        StartTime = data.StartTime;
        IsSqlServer = command.GetType().Name == "SqlCommand";
        Duration = data.Duration;
        HasTransaction = command.Transaction != null;
        Exception = exception;

        var parameters = command.Parameters.ToDictionary();
        var text = command.CommandText.Trim();
        NormalizeDescriptiveParameterNames(ref parameters, ref text);
        Parameters = parameters;
        Text = text;
    }

    // When using descriptive parameter names, the generator produces:
    //   first occurrence: @Id (no suffix)
    //   subsequent: @Id1, @Id2, etc.
    // This normalizes to 0-based numbering for duplicates:
    //   @Id → @Id0 (when @Id1 also exists)
    // while leaving singletons without a suffix.
    static void NormalizeDescriptiveParameterNames(
        ref Dictionary<string, object> parameters,
        ref string text)
    {
        // Dictionary keys are formatted as "@Name (Type)"
        // Find parameter names (the @Name part) that need renaming
        List<(string oldKey, string newKey, string oldParamName, string newParamName)>? renames = null;

        foreach (var key in parameters.Keys)
        {
            var spaceIndex = key.IndexOf(' ');
            if (spaceIndex < 0)
            {
                continue;
            }

            var paramName = key[..spaceIndex];
            var suffix = key[spaceIndex..];
            var paramName1 = paramName + "1";

            // Check if paramName + "1" exists as a parameter name
            var hasDuplicate = false;
            foreach (var otherKey in parameters.Keys)
            {
                if (otherKey.StartsWith(paramName1) &&
                    otherKey.Length > paramName1.Length &&
                    otherKey[paramName1.Length] == ' ')
                {
                    hasDuplicate = true;
                    break;
                }
            }

            if (!hasDuplicate)
            {
                continue;
            }

            renames ??= [];
            var newParamName = paramName + "0";
            renames.Add((key, newParamName + suffix, paramName, newParamName));
        }

        if (renames is null)
        {
            return;
        }

        foreach (var (oldKey, newKey, oldParamName, newParamName) in renames)
        {
            var value = parameters[oldKey];
            parameters.Remove(oldKey);
            parameters[newKey] = value;
            text = Regex.Replace(text, Regex.Escape(oldParamName) + @"(?!\w)", newParamName);
        }
    }
}
