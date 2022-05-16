public static class ModuleInitializer
{
    [ModuleInitializer]
    public static void Init()
    {
        VerifySqlServer.Enable();

        #region EnableCore

        VerifyEntityFramework.Enable();

        #endregion
    }
}