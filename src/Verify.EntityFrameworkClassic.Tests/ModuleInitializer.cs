public static class ModuleInitializer
{
    #region EnableClassic

    [ModuleInitializer]
    public static void Init() =>
        VerifyEntityFrameworkClassic.Enable();

    #endregion

    [ModuleInitializer]
    public static void InitOther() =>
        VerifyDiffPlex.Initialize();
}