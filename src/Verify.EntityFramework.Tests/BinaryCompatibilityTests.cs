public class BinaryCompatibilityTests
{
    // assemblies compiled against the pre optional parameter overloads, for example EfLocalDb,
    // bind to these exact signatures, and throw MissingMethodException if they are removed
    [Test]
    public Task EnableRecording() =>
        AssertEnableRecordingExists();

    [Test]
    public Task EnableRecordingWithIdentifier() =>
        AssertEnableRecordingExists(typeof(string));

    static async Task AssertEnableRecordingExists(params Type[] extraParameters)
    {
        var method = typeof(VerifyEntityFramework)
            .GetMethods()
            .SingleOrDefault(_ => _.Name == nameof(VerifyEntityFramework.EnableRecording) &&
                                  _.GetParameters()
                                      .Skip(1)
                                      .Select(_ => _.ParameterType)
                                      .SequenceEqual(extraParameters));
        await Assert.That(method).IsNotNull();
        await Assert.That(method!.ReturnType.GetGenericTypeDefinition()).IsEqualTo(typeof(DbContextOptionsBuilder<>));
    }
}
