using Verify.EntityFramework;
using Xunit;

[GlobalSetUp]
public static class GlobalSetup
{
    public static void Setup()
    {
        #region Enable
        VerifyEntityFramework.Enable();
        #endregion
    }
}