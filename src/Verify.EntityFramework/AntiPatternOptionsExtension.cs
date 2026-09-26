// Registers AntiPatternInterceptor in the service provider that EF builds. AddInterceptors is not used, since EF
// rejects a singleton interceptor added that way when the context uses UseInternalServiceProvider. EF does not apply
// extension services to such a provider, so the interceptor is then skipped instead.
// Since the extension is part of the service provider identity, a context without it never shares compiled queries
// with one that has it.
// The options are read from each context's options at runtime, so contexts with different options can share a
// service provider.
class AntiPatternOptionsExtension(AntiPatternOptions options) :
    IDbContextOptionsExtension
{
    public AntiPatternOptions Options { get; } = options;

    public DbContextOptionsExtensionInfo Info => field ??= new ExtensionInfo(this);

    public void ApplyServices(IServiceCollection services)
    {
        services.AddSingleton<IInterceptor>(AntiPatternInterceptor.Instance);
        services.AddSingleton<IInterceptor>(RuntimeAntiPatternInterceptor.Instance);
        // materialization interceptors are read from the ISingletonInterceptor services, not the IInterceptor ones
        services.AddSingleton<ISingletonInterceptor>(RuntimeAntiPatternInterceptor.Instance);
        services.AddScoped<AntiPatternState>();
    }

    public void Validate(IDbContextOptions options)
    {
    }

    class ExtensionInfo(IDbContextOptionsExtension extension) :
        DbContextOptionsExtensionInfo(extension)
    {
        public override bool IsDatabaseProvider => false;

        public override string LogFragment => "";

        public override int GetServiceProviderHashCode() => 0;

        public override bool ShouldUseSameServiceProvider(DbContextOptionsExtensionInfo other) =>
            other is ExtensionInfo;

        public override void PopulateDebugInfo(IDictionary<string, string> debugInfo)
        {
        }
    }
}
