namespace Verify.EntityFramework
{
    public static class VerifyEntityFramework
    {
        public static void Enable()
        {
            SharedVerifySettings.ModifySerialization(settings =>
            {
                settings.AddExtraSettings(serializer =>
                {
                    var converters = serializer.Converters;
                    converters.Add(new EntityEntryConverter());
                });
            });
        }
    }
}