using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Conventions.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;

internal class VerifyEfOptionsExtension : IDbContextOptionsExtension
{
    private DbContextOptionsExtensionInfo? _info;

    public string? InstanceName { get; set; }
    public DbContextOptionsExtensionInfo Info => _info ??= new ExtensionInfo(this);

    public VerifyEfOptionsExtension() { }

    public VerifyEfOptionsExtension(VerifyEfOptionsExtension copyFrom)
    {
        InstanceName = copyFrom.InstanceName;
    }

    public VerifyEfOptionsExtension Clone()
        => new VerifyEfOptionsExtension(this);

    public static VerifyEfOptionsExtension Extract(IDbContextOptions options)
    {
        var logOptionsExtension
            = options.Extensions
                .OfType<VerifyEfOptionsExtension>()
                .ToList();

        if (logOptionsExtension.Count == 0)
        {
            throw new InvalidOperationException($"No {nameof(VerifyEfOptionsExtension)} found.");
        }

        if (logOptionsExtension.Count > 1)
        {
            throw new InvalidOperationException($"Multiple {nameof(VerifyEfOptionsExtension)} found.");
        }

        return logOptionsExtension[0];
    }

    public void ApplyServices(IServiceCollection services)
    {
        services.AddSingleton<IModelCacheKeyFactory, VerifyEfModelCacheKeyFactory>();
        services.AddScoped<IConventionSetPlugin, VerifyEfConventionSetPlugin>();
    }

    public void Validate(IDbContextOptions options) { }

    public virtual VerifyEfOptionsExtension WithInstanceName(string? instanceName)
    {
        var clone = Clone();

        clone.InstanceName = instanceName;

        return clone;
    }

    private sealed class ExtensionInfo : DbContextOptionsExtensionInfo
    {
        public ExtensionInfo(IDbContextOptionsExtension extension)
            : base(extension)
        { }

        public override bool IsDatabaseProvider
            => false;

        public override string LogFragment
            => "using Verify.EntityFramework ";

        public override int GetServiceProviderHashCode()
            => 0;

        public override void PopulateDebugInfo(IDictionary<string, string> debugInfo)
            => debugInfo["Verify:EntityFramework"] = "1";

        public override bool ShouldUseSameServiceProvider(DbContextOptionsExtensionInfo other)
            => other is ExtensionInfo;
    }
}

internal class VerifyEfConventionSetPlugin : IConventionSetPlugin
{
    readonly IDbContextOptions contextOptions;

    public VerifyEfConventionSetPlugin(
        IDbContextOptions contextOptions)
    {
        this.contextOptions = contextOptions;
    }

    public ConventionSet ModifyConventions(ConventionSet conventionSet)
    {
        var extension = VerifyEfOptionsExtension.Extract(contextOptions);

        conventionSet.ModelFinalizedConventions.Add(new VerifyEfModelFinalizedConvention(extension.InstanceName!));

        return conventionSet;
    }
}

internal class VerifyEfModelFinalizedConvention : IModelFinalizedConvention
{
    readonly string instanceName;

    public VerifyEfModelFinalizedConvention(string instanceName)
    {
        this.instanceName = instanceName;
    }

    public IModel ProcessModelFinalized(IModel model)
    {
        model.AddRuntimeAnnotation("Verify:EntityFramework:Instance", instanceName);

        return model;
    }
}

internal class VerifyEfModelCacheKeyFactory : IModelCacheKeyFactory
{
    public virtual object Create(DbContext context, bool designTime)
        => new VerifyEfModelCacheKey(context, designTime);
}

internal class VerifyEfModelCacheKey
{
    private readonly string? _instanceName;
    private readonly Type _dbContextType;
    private readonly bool _designTime;

    public VerifyEfModelCacheKey(DbContext context, bool designTime)
    {
        _dbContextType = context.GetType();
        _instanceName = VerifyEfOptionsExtension.Extract(context.GetService<IDbContextOptions>()).InstanceName;
        _designTime = designTime;
    }

    protected bool Equals(VerifyEfModelCacheKey other)
        => _dbContextType == other._dbContextType
            && _designTime == other._designTime
            && _instanceName == other._instanceName;

    public override bool Equals(object? obj)
        => (obj is VerifyEfModelCacheKey otherAsKey) && Equals(otherAsKey);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(_dbContextType);
        hash.Add(_designTime);
        hash.Add(_instanceName);
        return hash.ToHashCode();
    }
}