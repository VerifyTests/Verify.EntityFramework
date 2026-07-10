[TestFixture]
public class RecordingDisabledTests
{
    static SqlInstance<SampleDbContext> sqlInstance = new(
        buildTemplate: data => data.Database.EnsureCreatedAsync(),
        storage: Storage.FromSuffix<SampleDbContext>("RecordingDisabled"),
        constructInstance: builder =>
        {
            builder.EnableRecording();
            return new(builder.Options);
        });

    static SqlInstance<SampleDbContext> identifierInstance = new(
        buildTemplate: data => data.Database.EnsureCreatedAsync(),
        storage: Storage.FromSuffix<SampleDbContext>("RecordingDisabledIdentifier"),
        constructInstance: builder =>
        {
            builder.EnableRecording("theIdentifier");
            return new(builder.Options);
        });

    [Test]
    public async Task ExecutedCommandIsNotRecorded()
    {
        await using var database = await sqlInstance.Build();
        var data = database.Context;

        Recording.Start();

        await data
            .Companies
            .Where(_ => _.Name == "Title")
            .ToListAsync();

        Assert.That(Recording.Stop(), Is.Empty);
    }

    [Test]
    public async Task ExecutedCommandIsNotRecordedForIdentifier()
    {
        await using var database = await identifierInstance.Build();
        var data = database.Context;

        Recording.Start("theIdentifier");

        await data
            .Companies
            .Where(_ => _.Name == "Title")
            .ToListAsync();

        Assert.That(Recording.Stop("theIdentifier"), Is.Empty);
    }

    // recordCommands only disables the recording interceptor, not the converters
    [Test]
    public async Task ConvertersAreStillRegistered()
    {
        await using var database = await sqlInstance.Build();
        var data = database.Context;

        var queryable = data
            .Companies
            .Where(_ => _.Name == "Title");

        await Verify(queryable);
    }
}
