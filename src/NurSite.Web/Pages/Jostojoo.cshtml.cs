using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using NurSite.Application.DTOs;
using NurSite.Application.Interfaces;

namespace NurSite.Web.Pages;

public class JostojooModel(ISearchService search) : PageModel
{
    [BindProperty(SupportsGet = true, Name = "q")] public string? Query { get; set; }
    [BindProperty(SupportsGet = true, Name = "type")] public SearchKind Kind { get; set; } = SearchKind.All;
    [BindProperty(SupportsGet = true, Name = "page")] public int PageNumber { get; set; } = 1;

    public SearchResponse? Result { get; private set; }
    public bool HasQuery => !string.IsNullOrWhiteSpace(Query);

    public async Task OnGetAsync(CancellationToken ct)
    {
        if (!HasQuery) return;

        Result = await search.SearchAsync(Query, Kind, PageNumber, pageSize: 10, ct);
    }

    /// <summary>واژه‌های یافته‌شده را در متن برجسته می‌کند.</summary>
    public Microsoft.AspNetCore.Html.IHtmlContent Highlight(string text)
    {
        if (Result is null || Result.Terms.Count == 0)
            return new Microsoft.AspNetCore.Html.HtmlString(
                System.Net.WebUtility.HtmlEncode(text));

        var encoded = System.Net.WebUtility.HtmlEncode(text);

        foreach (var term in Result.Terms.OrderByDescending(t => t.Length))
        {
            encoded = System.Text.RegularExpressions.Regex.Replace(
                encoded,
                System.Text.RegularExpressions.Regex.Escape(term),
                match => $"<mark>{match.Value}</mark>",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        }

        return new Microsoft.AspNetCore.Html.HtmlString(encoded);
    }
}