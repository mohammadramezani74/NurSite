using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Razor.TagHelpers;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;

namespace NurSite.Web.TagHelpers;

/// <summary>
/// نشانه‌گذاری ساختاریافته. یک شیء دلخواه را به اسکریپت JSON-LD تبدیل می‌کند.
/// استفاده: &lt;json-ld data="@Model.FaqSchema" /&gt;
/// </summary>
[HtmlTargetElement("json-ld", TagStructure = TagStructure.WithoutEndTag)]
public class JsonLdTagHelper : TagHelper
{
    public object? Data { get; set; }

    private static readonly JsonSerializerOptions Options = new()
    {
        // بدون این تنظیم، متن فارسی به کدهای یونیکد تبدیل می‌شود و حجم بی‌دلیل بالا می‌رود
        Encoder = JavaScriptEncoder.Create(UnicodeRanges.All),
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false
    };

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        if (Data is null)
        {
            output.SuppressOutput();
            return;
        }

        var json = JsonSerializer.Serialize(Data, Options);
        // بستن ناخواسته تگ اسکریپت را خنثی کن
        json = json.Replace("</", "<\\/");

        output.TagName = null;
        output.Content.SetHtmlContent(new HtmlString(
            $"<script type=\"application/ld+json\">{json}</script>"));
    }
}
