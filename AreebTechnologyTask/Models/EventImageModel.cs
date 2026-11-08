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
    /// <param name="Card">True for the 200px-tall event cards: uses the 2:1 "-card-400/-card-700" WebP crops.
    /// False uses the full-shape "-400/-800" WebP versions (Details page).</param>
    public record EventImageModel(
        Event Event,
        string CssClass,
        string Style,
        string Sizes,
        bool HighPriority = false,
        bool Lazy = false,
        bool Card = false);
}
