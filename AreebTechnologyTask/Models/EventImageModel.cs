namespace AreebTechnologyTask.Models
{
    /// <summary>
    /// Options for the shared Views/Shared/_EventImage.cshtml partial.
    /// </summary>
    /// <param name="Event">The event whose image is shown.</param>
    /// <param name="CssClass">CSS classes for the &lt;img&gt;.</param>
    /// <param name="Style">Inline style for the &lt;img&gt;.</param>
    /// <param name="Sizes">The "sizes" attribute: how wide the image is shown, so the browser picks the right file.</param>
    /// <param name="HighPriority">True for the main (LCP) image of the page: fetched first.</param>
    /// <param name="Lazy">True for images below the fold: loaded only when scrolled near.</param>
    public record EventImageModel(
        Event Event,
        string CssClass,
        string Style,
        string Sizes,
        bool HighPriority = false,
        bool Lazy = false);
}
