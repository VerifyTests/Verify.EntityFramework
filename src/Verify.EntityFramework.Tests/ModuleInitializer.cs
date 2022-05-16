public static class ModuleInitializer
{
    [ModuleInitializer]
    public static void Init()
    {
        #region EnableCore

        VerifyEntityFramework.Enable();

        #endregion
    }
}