[TestFixture]
[Parallelizable(ParallelScope.All)]
public class QueryableTests
{
    // Include returns an IIncludableQueryable, which was not recognized, so no sql was written
    [Test]
    public async Task IncludeLast()
    {
        await using var database = await DbContextBuilder.GetDatabase();
        var data = database.Context;
        var queryable = data.Companies
            .Where(_ => _.Name == "Company1")
            .Include(_ => _.Employees);
        await Verify(queryable);
    }

    [Test]
    public async Task ThenIncludeLast()
    {
        await using var database = await DbContextBuilder.GetDatabase();
        var data = database.Context;
        var queryable = data.Employees
            .Where(_ => _.Name == "Employee1")
            .Include(_ => _.Company)
            .ThenInclude(_ => _.Employees);
        await Verify(queryable)
            .IgnoreNavigationProperties();
    }

    [Test]
    public async Task DbSet()
    {
        await using var database = await DbContextBuilder.GetDatabase();
        var data = database.Context;
        await Verify(data.Companies);
    }

    [Test]
    public async Task NestedInclude()
    {
        await using var database = await DbContextBuilder.GetDatabase();
        var data = database.Context;
        var queryable = data.Companies
            .Where(_ => _.Name == "Company1")
            .Include(_ => _.Employees);
        await Verify(
            new
            {
                queryable
            });
    }
}
