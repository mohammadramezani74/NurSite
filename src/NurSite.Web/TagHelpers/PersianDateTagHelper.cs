using Microsoft.AspNetCore.Razor.TagHelpers;
using NurSite.Application.Interfaces;

namespace NurSite.Web.TagHelpers;

/// <summary>
/// تاریخ UTC را به شمسی نمایش می‌دهد و در datetime تاریخ استاندارد ISO می‌گذارد
/// تا موتور جستجو آن را درست بخواند.
/// استفاده: &lt;persian-date value="@Model.PublishedAtUtc" weekday="true" /&gt;
/// </summary>
[HtmlTargetElement("persian-date", TagStructure = TagStructure.NormalOrSelfClosing)]
public class PersianDateTagHelper(IPersianDateService dates) : TagHelper
{
    public DateTime? Value { get; set; }
    public bool Weekday { get; set; }
    public bool Time { get; set; }

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        if (Value is null)
        {
            output.SuppressOutput();
            return;
        }

        output.TagName = "time";
        output.TagMode = TagMode.StartTagAndEndTag;
        output.Attributes.SetAttribute("datetime", Value.Value.ToUniversalTime().ToString("o"));
        output.Content.SetContent(dates.ToPersianDate(Value.Value, Weekday, Time));
    }
}

/// <summary>ارقام لاتین داخل تگ را فارسی می‌کند. &lt;fa-num&gt;1405&lt;/fa-num&gt;</summary>
[HtmlTargetElement("fa-num")]
public class PersianDigitsTagHelper(IPersianDateService dates) : TagHelper
{
    public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
    {
        var content = await output.GetChildContentAsync();
        output.TagName = null; // خود تگ در خروجی نمی‌ماند
        output.Content.SetContent(dates.ToPersianDigits(content.GetContent()));
    }
}
