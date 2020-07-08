using VerifyTests;

public static class ModuleInitializer
{
    public static void Initialize()
    {
        #region EnableCore
        VerifyEntityFramework.Enable();
        VerifierSettings.DisableNewLineEscaping();
        #endregion
    }
}