class ChangePair
{
    public ChangePair(string key, object? original, object? current)
    {
        Key = key;
        Original = original;
        Current = current;
    }

    public string Key { get; }
    public object? Original { get; }
    public object? Current { get; }
}