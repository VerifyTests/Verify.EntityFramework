using System;

namespace Verify
{
    public static class VerifySettingsExtensions
    {
        public static void SchemaSettings(
            this VerifySettings settings,
            bool storedProcedures = true,
            bool tables = true,
            bool views = true,
            Func<string, bool>? includeItem = null)
        {
            Guard.AgainstNull(settings, nameof(settings));
            if (includeItem == null)
            {
                includeItem = s => true;
            }

            settings.Data.Add("EntityFramework",
                new SchemaSettings(
                    storedProcedures,
                    tables,
                    views,
                    includeItem));
        }

        internal static SchemaSettings GetSchemaSettings(this VerifySettings settings)
        {
            Guard.AgainstNull(settings, nameof(settings));
            if (settings.Data.TryGetValue("EntityFramework", out var value))
            {
                return (SchemaSettings) value;
            }

            return defaultSettings;
        }

        static SchemaSettings defaultSettings = new SchemaSettings(true, true, true, s => true);
    }
}