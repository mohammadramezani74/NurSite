using NurSite.Application.DTOs;

namespace NurSite.Application.Interfaces;

public interface ISearchService
{
    Task<SearchResponse> SearchAsync(
        string? query,
        SearchKind kind = SearchKind.All,
        int page = 1,
        int pageSize = 10,
        CancellationToken ct = default);

    /// <summary>پیشنهاد لحظه‌ای برای جعبه جستجو.</summary>
    Task<IReadOnlyList<SearchHit>> SuggestAsync(string? query, int take = 6, CancellationToken ct = default);
}