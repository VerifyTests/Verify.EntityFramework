// Registers RecordingQueryCompiler and RecordingStateManager, which record what InMemory queries and saves
class RecordingOptionsExtension(LogCommandInterceptor interceptor) :
    IDbContextOptionsExtension
{
    public LogCommandInterceptor Interceptor { get; } = interceptor;

    public DbContextOptionsExtensionInfo Info => field ??= new ExtensionInfo(this);

    public void ApplyServices(IServiceCollection services)
    {
        ReplaceDefault<IQueryCompiler, QueryCompiler, RecordingQueryCompiler>(services);
        ReplaceDefault<IStateManager, StateManager, RecordingStateManager>(services);
    }

    // Only replace the implementation that EF registers, so a library that replaces or decorates the service keeps
    // working. No registration means the provider services are applied after this extension, and TryAdd will then
    // skip registering the default.
    static void ReplaceDefault<TService, TDefault, TReplacement>(IServiceCollection services)
        where TService : class
        where TReplacement : class, TService
    {
        var descriptor = services.FirstOrDefault(_ => _.ServiceType == typeof(TService));
        if (descriptor == null ||
            descriptor.ImplementationType == typeof(TDefault))
        {
            services.Replace(ServiceDescriptor.Scoped<TService, TReplacement>());
        }
    }

    public void Validate(IDbContextOptions options)
    {
    }

    class ExtensionInfo(IDbContextOptionsExtension extension) :
        DbContextOptionsExtensionInfo(extension)
    {
        public override bool IsDatabaseProvider => false;

        public override string LogFragment => "";

        // the interceptor is read from the options of each context, so all contexts can share a service provider
        public override int GetServiceProviderHashCode() => 0;

        public override bool ShouldUseSameServiceProvider(DbContextOptionsExtensionInfo other) =>
            other is ExtensionInfo;

        public override void PopulateDebugInfo(IDictionary<string, string> debugInfo)
        {
        }
    }
}
