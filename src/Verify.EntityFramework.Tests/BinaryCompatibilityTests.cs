[TestFixture]
[Parallelizable(ParallelScope.All)]
public class BinaryCompatibilityTests
{
    // assemblies compiled against the pre optional parameter overloads, for example EfLocalDb,
    // bind to these exact signatures, and throw MissingMethodException if they are removed
    [Test]
    public void EnableRecording() =>
        AssertEnableRecordingExists();

    [Test]
    public void EnableRecordingWithIdentifier() =>
        AssertEnableRecordingExists(typeof(string));

    static void AssertEnableRecordingExists(params Type[] extraParameters)
    {
        var method = typeof(VerifyEntityFramework)
            .GetMethods()
            .SingleOrDefault(_ => _.Name == nameof(VerifyEntityFramework.EnableRecording) &&
                                  _.GetParameters()
                                      .Skip(1)
                                      .Select(_ => _.ParameterType)
                                      .SequenceEqual(extraParameters));
        Assert.That(method, Is.Not.Null);
        Assert.That(method!.ReturnType.GetGenericTypeDefinition(), Is.EqualTo(typeof(DbContextOptionsBuilder<>)));
    }
}
