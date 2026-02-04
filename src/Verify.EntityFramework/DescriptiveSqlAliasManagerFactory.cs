#pragma warning disable EF9002

class DescriptiveSqlAliasManagerFactory : ISqlAliasManagerFactory
{
    public SqlAliasManager Create() => new DescriptiveSqlAliasManager();
}
