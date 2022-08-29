public static class ModuleInitializer
{
    #region EnableClassic

    [ModuleInitializer]
    public static void Init()
    {

        VerifyEntityFrameworkClassic.Enable();

        #endregion

        VerifyDiffPlex.Initialize();
    }
}