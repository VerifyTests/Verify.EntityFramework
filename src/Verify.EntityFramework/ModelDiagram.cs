using DbToMermaid;

namespace VerifyTests;

public static class VerifyEntityFrameworkModelDiagram
{
    const string contextKey = "EntityFramework.ModelDiagram";
    const string filterKey = "EntityFramework.ModelDiagramFilter";
    const string namesOnlyKey = "EntityFramework.ModelDiagramNamesOnly";

    /// <summary>
    /// Select the files produced when an <see cref="IModel" /> is verified as a Mermaid ER diagram.
    /// </summary>
    /// <param name="settings">The settings to apply to.</param>
    /// <param name="formats">The files to produce. Can be combined, with one file verified per format.</param>
    /// <param name="namesOnly">When true, only the table names are rendered, without columns.</param>
    public static SettingsTask ModelAsDiagram(
        this SettingsTask settings,
        ModelDiagramFormat formats = ModelDiagramFormat.Markdown,
        bool namesOnly = false)
    {
        settings.CurrentSettings.ModelAsDiagram(formats, namesOnly);
        return settings;
    }

    /// <inheritdoc cref="ModelAsDiagram(SettingsTask, ModelDiagramFormat, bool)" />
    public static void ModelAsDiagram(
        this VerifySettings settings,
        ModelDiagramFormat formats = ModelDiagramFormat.Markdown,
        bool namesOnly = false)
    {
        if ((formats & ModelDiagramFormat.All) == 0)
        {
            throw new ArgumentException("At least one format is required.", nameof(formats));
        }

        settings.Context[contextKey] = formats;
        settings.Context[namesOnlyKey] = namesOnly;
    }

    /// <summary>
    /// Select the entity types included when an <see cref="IModel" /> is verified as a Mermaid ER diagram.
    /// Foreign keys to an excluded entity type are omitted.
    /// </summary>
    /// <param name="settings">The settings to apply to.</param>
    /// <param name="filter">Returns true for the entity types to include.</param>
    public static SettingsTask ModelDiagramFilter(
        this SettingsTask settings,
        Func<IEntityType, bool> filter)
    {
        settings.CurrentSettings.ModelDiagramFilter(filter);
        return settings;
    }

    /// <inheritdoc cref="ModelDiagramFilter(SettingsTask, Func{IEntityType, bool})" />
    public static void ModelDiagramFilter(
        this VerifySettings settings,
        Func<IEntityType, bool> filter) =>
        settings.Context[filterKey] = filter;

    internal static async Task<ConversionResult> Convert(IModel model, IReadOnlyDictionary<string, object> context)
    {
        var formats = ModelDiagramFormat.Markdown;
        if (context.TryGetValue(contextKey, out var value))
        {
            formats = (ModelDiagramFormat) value;
        }

        var namesOnly = context.TryGetValue(namesOnlyKey, out var namesOnlyValue) &&
                        (bool) namesOnlyValue;

        Func<IEntityType, bool>? filter = null;
        if (context.TryGetValue(filterKey, out var filterValue))
        {
            filter = (Func<IEntityType, bool>) filterValue;
        }

        var targets = new List<Target>();

        if (formats.HasFlag(ModelDiagramFormat.Markdown))
        {
            var markdown = await EfToMermaid.RenderMarkdown(model, filter, namesOnly);
            targets.Add(new("md", markdown));
        }

        if (formats.HasFlag(ModelDiagramFormat.Svg))
        {
            var svg = await EfToMermaid.RenderSvg(model, filter, namesOnly);
            targets.Add(new("svg", svg));
        }

        if (formats.HasFlag(ModelDiagramFormat.Png))
        {
            var png = await EfToMermaid.RenderPng(model, filter, namesOnly);
            targets.Add(new("png", new MemoryStream(png)));
        }

        return new(null, targets);
    }
}
