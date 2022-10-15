public static class ModuleInitializer
{
    #region EnableCore

    [ModuleInitializer]
    public static void Init()
    {
        VerifyEntityFramework.Enable();

        #endregion

        VerifySqlServer.Enable();
        VerifyDiffPlex.Initialize();
    }
}