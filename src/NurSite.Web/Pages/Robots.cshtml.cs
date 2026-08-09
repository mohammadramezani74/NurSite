using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace NurSite.Web.Pages;

public class RobotsModel(IWebHostEnvironment env) : PageModel
{
    public IActionResult OnGet()
    {
        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        var sb = new StringBuilder();

        sb.AppendLine("User-agent: *");

        // در محیط توسعه یا استیجینگ هیچ‌چیز ایندکس نشود
        if (!env.IsProduction())
        {
            sb.AppendLine("Disallow: /");
            return Content(sb.ToString(), "text/plain", Encoding.UTF8);
        }

        sb.AppendLine("Disallow: /admin");
        sb.AppendLine("Disallow: /vorood");
        sb.AppendLine("Disallow: /khorooj");
        sb.AppendLine("Disallow: /dastresi-nadarid");
        sb.AppendLine("Disallow: /offline");
        sb.AppendLine("Disallow: /Identity/");
        sb.AppendLine();
        sb.AppendLine($"Sitemap: {baseUrl}/sitemap.xml");

        return Content(sb.ToString(), "text/plain", Encoding.UTF8);
    }
}