class QueryEntry(string type, string text, Dictionary<string, object?> parameters)
{
    public string Type { get; } = type;
    public Dictionary<string, object?> Parameters { get; } = parameters;
    public string Text { get; } = text;
}
