namespace VerifyTests.EntityFramework;

/// <summary>
/// The files produced when a model is verified as a diagram. Can be combined.
/// </summary>
[Flags]
public enum ModelDiagramFormat
{
    /// <summary>
    /// A Mermaid ER diagram in a markdown code block. Uses the `md` extension.
    /// </summary>
    Markdown = 1,

    /// <summary>
    /// The diagram rendered to an image. Uses the `svg` extension.
    /// </summary>
    Svg = 2,

    /// <summary>
    /// The diagram rendered to an image. Uses the `png` extension.
    /// </summary>
    Png = 4,

    All = Markdown | Svg | Png
}
