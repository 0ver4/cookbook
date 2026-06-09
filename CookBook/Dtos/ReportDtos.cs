using CookBook.Models;

namespace CookBook.Dtos;

/// <summary>Zgłoszenie przepisu w widoku moderacji.</summary>
public record RecipeReportDto(
    int Id,
    int RecipeId,
    string RecipeName,
    string ReportedByName,
    string Reason,
    ReportStatus Status,
    DateTime CreatedAt,
    string? ResolvedByName,
    DateTime? ResolvedAt);

/// <summary>Zgłoszenie komentarza w widoku moderacji.</summary>
public record CommentReportDto(
    int Id,
    int CommentId,
    int RecipeId,
    string CommentContent,
    string CommentAuthorName,
    string ReportedByName,
    string Reason,
    ReportStatus Status,
    DateTime CreatedAt,
    string? ResolvedByName,
    DateTime? ResolvedAt);

/// <summary>Komplet danych dla panelu moderacji zgłoszeń.</summary>
public record ReportsViewModel(
    IReadOnlyList<RecipeReportDto> RecipeReports,
    IReadOnlyList<CommentReportDto> CommentReports,
    bool ShowResolved);
