using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Razor.TagHelpers;
using System.Text;
using System.Text.Encodings.Web;

namespace NurSite.Web.TagHelpers;

/// <summary>
/// همه متاتگ‌های سئو را یکجا تولید می‌کند: توضیحات، canonical، OpenGraph و کارت توییتر.
/// استفاده در Layout: &lt;seo-meta title="..." description="..." canonical="..." /&gt;
/// </summary>
[HtmlTargetElement("seo-meta", TagStructure = TagStructure.WithoutEndTag)]
public class SeoMetaTagHelper : TagHelper
{
    public string Title { get; set; } = default!;
    public string? Description { get; set; }
    public string? Canonical { get; set; }
    public string? Image { get; set; }
    public string SiteName { get; set; } = "";
    public string OgType { get; set; } = "website";
    public bool NoIndex { get; set; }

    /// <summary>برای مقالات: زمان انتشار و آخرین ویرایش.</summary>
    public DateTime? PublishedAtUtc { get; set; }
    public DateTime? ModifiedAtUtc { get; set; }

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        var sb = new StringBuilder();
        var enc = HtmlEncoder.Default;

        void Meta(string attr, string key, string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return;
            sb.Append($"<meta {attr}=\"{enc.Encode(key)}\" content=\"{enc.Encode(value)}\">\n");
        }

        if (!string.IsNullOrWhiteSpace(Description))
            Meta("name", "description", Description);

        if (NoIndex)
            Meta("name", "robots", "noindex, nofollow");

        if (!string.IsNullOrWhiteSpace(Canonical))
            sb.Append($"<link rel=\"canonical\" href=\"{enc.Encode(Canonical)}\">\n");

        // OpenGraph
        Meta("property", "og:title", Title);
        Meta("property", "og:description", Description);
        Meta("property", "og:type", OgType);
        Meta("property", "og:url", Canonical);
        Meta("property", "og:image", Image);
        Meta("property", "og:site_name", SiteName);
        Meta("property", "og:locale", "fa_IR");

        if (PublishedAtUtc is not null)
            Meta("property", "article:published_time", PublishedAtUtc.Value.ToUniversalTime().ToString("o"));
        if (ModifiedAtUtc is not null)
            Meta("property", "article:modified_time", ModifiedAtUtc.Value.ToUniversalTime().ToString("o"));

        // کارت توییتر
        Meta("name", "twitter:card", string.IsNullOrWhiteSpace(Image) ? "summary" : "summary_large_image");
        Meta("name", "twitter:title", Title);
        Meta("name", "twitter:description", Description);
        Meta("name", "twitter:image", Image);

        output.TagName = null;
        output.Content.SetHtmlContent(new HtmlString(sb.ToString()));
    }
}
