using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using VerifyXunit;
using Xunit;
using Xunit.Abstractions;

public class MyControllerTests :
    VerifyBase
{
    #region MyControllerTest
    [Fact]
    public Task Test()
    {
        var context = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
        var controller = new MyController
        {
            ControllerContext = context
        };

        var result = controller.Method("inputValue");
        return Verify(
            new
            {
                result,
                context
            });
    }
    #endregion

    public MyControllerTests(ITestOutputHelper output) :
        base(output)
    {
    }
}