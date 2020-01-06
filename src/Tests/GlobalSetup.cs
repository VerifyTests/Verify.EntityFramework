using Verify.Web;
using Xunit;

[GlobalSetUp]
public static class GlobalSetup
{
    public static void Setup()
    {
        #region Enable
        VerifyWeb.Enable();
        #endregion
    }
}