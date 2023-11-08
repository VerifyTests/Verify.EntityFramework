namespace VerifyTests.EntityFramework;

[Obsolete("Use VerifyTest.Recording",true)]
public static class EfRecording
{
    [Obsolete("Use VerifyTest.Recording.Start()",true)]
    public static void StartRecording()
    {
    }

    [Obsolete("Use VerifyTest.Recording.Start(string identifier)",true)]
    public static void StartRecording(string identifier)
    {
    }

    [Obsolete("Use VerifyTest.Recording.Stop()",true)]
    public static IReadOnlyList<LogEntry> FinishRecording() =>
        throw new NotImplementedException();

    [Obsolete("Use VerifyTest.Recording.Stop(string identifier)",true)]
    public static IReadOnlyList<LogEntry> FinishRecording(string identifier)=>
        throw new NotImplementedException();
}