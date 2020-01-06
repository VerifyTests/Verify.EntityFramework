using System.Threading.Tasks;
using VerifyXunit;
using Xunit;
using Xunit.Abstractions;

public class Usage :
    VerifyBase
{
    #region MyControllerTest
    [Fact]
    public async Task ChangeTracked()
    {
        var database = await DbContextBuilder.GetDatabase("ChangeTracked");
        var employee = await database.Context.Employees.FindAsync(3);
        employee.Age++;
        await Verify(database.Context);
    }
    #endregion

    public Usage(ITestOutputHelper output) :
        base(output)
    {
    }
}