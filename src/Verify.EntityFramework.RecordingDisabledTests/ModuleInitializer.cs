using Microsoft.EntityFrameworkCore.Metadata;

public static class ModuleInitializer
{
    [ModuleInitializer]
    public static void Init()
    {
        using var data = GetDbContext();

        #region DisableRecording

        VerifyEntityFramework.Initialize(data, recordCommands: false);

        #endregion
    }

    static SampleDbContext GetDbContext()
    {
        var options = new DbContextOptionsBuilder<SampleDbContext>();
        options.UseSqlServer("fake");
        return new(options.Options);
    }
}
