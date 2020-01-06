using VerifyXunit;
using Xunit.Abstractions;

public class Tests :
    VerifyBase
{
    public Tests(ITestOutputHelper output) :
        base(output)
    {
    }
}