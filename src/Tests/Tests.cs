using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using VerifyXunit;
using Xunit;
using Xunit.Abstractions;

public class Tests :
    VerifyBase
{

    [Fact]
    public Task ChallengeResult()
    {
        var result = new ChallengeResult(
            "scheme",
            new AuthenticationProperties(
                new Dictionary<string, string>
                {
                    {"key", "value"}
                }));
        return VerifyResult(result);
    }


    public Tests(ITestOutputHelper output) :
        base(output)
    {
    }
}