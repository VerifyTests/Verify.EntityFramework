using DbToMermaid;

namespace VerifyTests;

public static class VerifyEntityFrameworkModelDiagram
{
    const string contextKey = "EntityFramework.ModelDiagram";

    /// <summary>
    /// Select the files produced when an <see cref="IModel" /> is verified as a Mermaid ER diagram.
    /// </summary>
    /// <param name="settings">The settings to apply to.</param>
    /// <param name="formats">The files to produce. Can be combined, with one file verified per format.</param>
    public static SettingsTask ModelAsDiagram(
        this SettingsTask settings,
        ModelDiagramFormat formats = ModelDiagramFormat.Markdown)
    {
        settings.CurrentSettings.ModelAsDiagram(formats);
        return settings;
    }

    /// <inheritdoc cref="ModelAsDiagram(SettingsTask, ModelDiagramFormat)" />
    public static void ModelAsDiagram(
        this VerifySettings settings,
        ModelDiagramFormat formats = ModelDiagramFormat.Markdown)
    {
        if ((formats & ModelDiagramFormat.All) == 0)
        {
            throw new ArgumentException("At least one format is required.", nameof(formats));
        }

        settings.Context[contextKey] = formats;
    }

    internal static async Task<ConversionResult> Convert(IModel model, IReadOnlyDictionary<string, object> context)
    {
        var formats = ModelDiagramFormat.Markdown;
        if (context.TryGetValue(contextKey, out var value))
        {
            formats = (ModelDiagramFormat) value;
        }

        var targets = new List<Target>();

        if (formats.HasFlag(ModelDiagramFormat.Markdown))
        {
            var markdown = await EfToMermaid.RenderMarkdown(model);
            targets.Add(new("md", markdown));
        }

        if (formats.HasFlag(ModelDiagramFormat.Svg))
        {
            var svg = await EfToMermaid.RenderSvg(model);
            targets.Add(new("svg", svg));
        }

        if (formats.HasFlag(ModelDiagramFormat.Png))
        {
            var png = await EfToMermaid.RenderPng(model);
            targets.Add(new("png", new MemoryStream(png)));
        }

        return new(null, targets);
    }
}
