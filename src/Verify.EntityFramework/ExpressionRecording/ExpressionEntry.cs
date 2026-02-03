namespace VerifyTests.EntityFramework;

public class ExpressionEntry(string type, string expression)
{
    public string Type { get; } = type;
    public string Expression { get; } = expression;
}
