[TestFixture]
[NonParallelizable]
public class StaticSettingsTests
{
    static SqlInstance<SampleDbContext> sqlInstance = new(
        buildTemplate: data => data.Database.EnsureCreatedAsync(),
        constructInstance: builder =>
        {
            builder.EnableRecording();
            return new(builder.Options);
        });

    [TearDown]
    public void TearDown() =>
        VerifyEntityFramework.DisableSqlFormatting = false;

    [Test]
    public async Task DisableSqlFormattingRecording()
    {
        #region DisableSqlFormatting

        VerifyEntityFramework.DisableSqlFormatting = true;

        #endregion

        await using var database = await sqlInstance.Build();
        var data = database.Context;

        Recording.Start();

        await data
            .Companies
            .Where(_ => _.Name == "Title")
            .ToListAsync();

        await Verify();
    }

    [Test]
    public async Task DisableSqlFormattingQueryable()
    {
        VerifyEntityFramework.DisableSqlFormatting = true;

        await using var database = await sqlInstance.Build();
        var data = database.Context;

        var queryable = data
            .Companies
            .Where(_ => _.Name == "Title");

        await Verify(queryable);
    }
}
