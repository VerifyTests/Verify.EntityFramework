using Microsoft.EntityFrameworkCore.Metadata;

public static class ModuleInitializer
{
    static IModel GetDbModel()
    {
        var options = new DbContextOptionsBuilder<SampleDbContext>();
        options.UseSqlServer("fake");
        using var data = new SampleDbContext(options.Options);
        return data.Model;
    }

    [ModuleInitializer]
    public static void Init()
    {
        var model = GetDbModel();
        VerifyEntityFramework.Initialize(model);
        VerifierSettings.InitializePlugins();
        Recording.IgnoreNames("sql");
    }
}
