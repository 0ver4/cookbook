using CookBook.Dtos;
using CookBook.Models;

namespace CookBook.Services;

public interface IReportService
{
    // --- Zgłaszanie (użytkownicy) ---
    Task<(bool Success, string? Error)> ReportRecipeAsync(int recipeId, int userId, string reason);
    Task<(bool Success, string? Error)> ReportCommentAsync(int commentId, int userId, string reason);

    // --- Moderacja ---
    Task<ReportsViewModel> GetReportsAsync(bool includeResolved = false);

    /// <summary>Liczba oczekujących zgłoszeń (przepisy + komentarze) — do odznaki w nawigacji.</summary>
    Task<int> GetPendingCountAsync();

    Task<(bool Success, string? Error)> SetRecipeReportStatusAsync(int reportId, int moderatorId, ReportStatus status);
    Task<(bool Success, string? Error)> SetCommentReportStatusAsync(int reportId, int moderatorId, ReportStatus status);

    /// <summary>Usuwa zgłoszony przepis (kaskadowo znikają jego zgłoszenia).</summary>
    Task<(bool Success, string? Error)> DeleteReportedRecipeAsync(int reportId, int moderatorId);

    /// <summary>Usuwa zgłoszony komentarz (kaskadowo znikają jego zgłoszenia).</summary>
    Task<(bool Success, string? Error)> DeleteReportedCommentAsync(int reportId, int moderatorId);
}
