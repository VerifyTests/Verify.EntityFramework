using VerifyTests.EntityFramework;

public class ModelDiagramTests
{
    [Test]
    public async Task Markdown()
    {
        await using var data = BuildData();

        #region ModelDiagram

        await Verify(data.Model);

        #endregion
    }

    [Test]
    public async Task Formats()
    {
        await using var data = BuildData();

        #region ModelDiagramFormats

        await Verify(data.Model)
            .ModelAsDiagram(
                ModelDiagramFormat.Markdown |
                ModelDiagramFormat.Svg |
                ModelDiagramFormat.Png);

        #endregion
    }

    [Test]
    public async Task Filter()
    {
        await using var data = BuildData();

        #region ModelDiagramFilter

        await Verify(data.Model)
            // exclude the Employee entity
            .ModelDiagramFilter(_ => _.ClrType != typeof(Employee));

        #endregion
    }

    [Test]
    public async Task NamesOnly()
    {
        await using var data = BuildData();

        #region ModelDiagramNamesOnly

        await Verify(data.Model)
            .ModelAsDiagram(namesOnly: true);

        #endregion
    }

    [Test]
    public async Task SvgOnly()
    {
        await using var data = BuildData();
        await Verify(data.Model)
            .ModelAsDiagram(ModelDiagramFormat.Svg);
    }

    [Test]
    public async Task NoFormat()
    {
        var settings = new VerifySettings();
        await Assert.That(() => settings.ModelAsDiagram(0))
            .Throws<ArgumentException>();
    }

    static SampleDbContext BuildData()
    {
        var options = new DbContextOptionsBuilder<SampleDbContext>();
        options.UseSqlServer("fake");
        return new(options.Options);
    }
}
