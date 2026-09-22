using Ganss.Xss;

namespace NafasLand.Admin.Modules.Catalog.Infrastructure;

internal interface IProductHtmlSanitizer
{
    string? Sanitize(string? html);
}

internal sealed class ProductHtmlSanitizer : IProductHtmlSanitizer
{
    private static readonly string[] Tags =
    [
        "p", "br", "strong", "b", "em", "i", "u", "ul", "ol", "li", "a", "img",
        "h2", "h3", "h4", "table", "thead", "tbody", "tr", "td", "th", "span", "div",
    ];

    public string? Sanitize(string? html)
    {
        if (html is null)
        {
            return null;
        }

        var sanitizer = new HtmlSanitizer();
        sanitizer.AllowedTags.Clear();
        sanitizer.AllowedTags.UnionWith(Tags);
        sanitizer.AllowedAttributes.Clear();
        sanitizer.AllowedAttributes.UnionWith(["style", "href", "src", "alt", "title"]);
        sanitizer.AllowedCssProperties.Clear();
        sanitizer.AllowedCssProperties.UnionWith(["text-align", "font-weight"]);
        sanitizer.AllowedSchemes.Clear();
        sanitizer.AllowedSchemes.UnionWith(["http", "https", "mailto"]);
        sanitizer.KeepChildNodes = false;
        return sanitizer.Sanitize(html);
    }
}
