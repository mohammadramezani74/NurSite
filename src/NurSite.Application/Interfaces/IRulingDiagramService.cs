using NurSite.Application.Services;

namespace NurSite.Application.Interfaces;

public sealed record DiagramSaveResult(
    bool Ok,
    int NodeCount,
    int VerdictCount,
    IReadOnlyList<string> Errors,
    IReadOnlyList<string> UnknownMarjas);

public interface IRulingDiagramService
{
    /// <summary>درخت ذخیره‌شده را به متن تورفته برمی‌گرداند تا قابل ویرایش باشد.</summary>
    Task<string> ExportOutlineAsync(int rulingId, CancellationToken ct = default);

    /// <summary>متن تورفته را می‌خواند و درخت را جایگزین می‌کند.</summary>
    Task<DiagramSaveResult> SaveOutlineAsync(int rulingId, string? outline, CancellationToken ct = default);
}