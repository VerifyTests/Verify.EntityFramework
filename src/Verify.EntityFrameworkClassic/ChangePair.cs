class ChangePair(string key, object? original, object? current)
{
    public string Key { get; } = key;
    public object? Original { get; } = original;
    public object? Current { get; } = current;
}