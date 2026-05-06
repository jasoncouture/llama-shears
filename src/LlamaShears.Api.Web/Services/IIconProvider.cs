namespace LlamaShears.Api.Web.Services;

/// <summary>
/// Resolves a Bootstrap-Icons icon name (e.g. <c>arrow-clockwise</c>)
/// to the inner SVG body — the markup between the outer
/// <c>&lt;svg&gt;</c> tags. Callers wrap that body in their own
/// <c>&lt;svg&gt;</c> with whatever sizing/class/attribute set they
/// want, which keeps theming via <c>currentColor</c> straightforward.
/// </summary>
public interface IIconProvider
{
    /// <summary>
    /// Returns the inner SVG body for <paramref name="name"/>, or an
    /// empty string when no icon by that name is bundled.
    /// </summary>
    string GetInnerSvg(string name);
}
