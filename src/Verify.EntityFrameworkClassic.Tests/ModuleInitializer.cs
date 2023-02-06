public static class ModuleInitializer
{
    #region EnableClassic

    [ModuleInitializer]
    public static void Init() =>
        VerifyEntityFrameworkClassic.Initialize();

    #endregion

    [ModuleInitializer]
    public static void InitOther() =>
        VerifierSettings.InitializePlugins();
}