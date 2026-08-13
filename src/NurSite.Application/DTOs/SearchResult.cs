namespace NurSite.Application.DTOs;

public enum SearchKind
{
    All = 0,
    Article = 1,
    Ruling = 2,
    Lecture = 3
}

/// <summary>یک نتیجه جستجو، مستقل از اینکه مقاله باشد یا حکم.</summary>
public sealed record SearchHit(
    SearchKind Kind,
    string Title,
    string Snippet,
    string Url,
    string? CategoryTitle,
    DateTime? DateUtc,
    int Score);

public sealed record SearchResponse(
    string Query,
    IReadOnlyList<SearchHit> Hits,
    int TotalCount,
    int PageNumber,
    int PageSize,
    IReadOnlyList<string> Terms)
{
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
}