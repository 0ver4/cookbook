using CookBook.Dtos;
using CookBook.Models;
using CookBook.Repositories;
using Microsoft.EntityFrameworkCore;

namespace CookBook.Services;

public class ReportService : IReportService
{
    private const int MaxReasonLength = 1000;

    private readonly IRepository<RecipeReport> _recipeReports;
    private readonly IRepository<CommentReport> _commentReports;
    private readonly IRepository<Recipe> _recipes;
    private readonly IRepository<Comment> _comments;
    private readonly IRecipeService _recipeService;

    public ReportService(
        IRepository<RecipeReport> recipeReports,
        IRepository<CommentReport> commentReports,
        IRepository<Recipe> recipes,
        IRepository<Comment> comments,
        IRecipeService recipeService)
    {
        _recipeReports = recipeReports;
        _commentReports = commentReports;
        _recipes = recipes;
        _comments = comments;
        _recipeService = recipeService;
    }

    // --- Zgłaszanie ---

    public async Task<(bool Success, string? Error)> ReportRecipeAsync(int recipeId, int userId, string reason)
    {
        var (normalized, error) = NormalizeReason(reason);
        if (error is not null)
            return (false, error);

        if (!await _recipes.ExistsAsync(recipeId))
            return (false, "Nie znaleziono przepisu.");

        var alreadyReported = await _recipeReports.Query()
            .AnyAsync(r => r.RecipeId == recipeId && r.ReportedById == userId && r.Status == ReportStatus.Pending);
        if (alreadyReported)
            return (false, "Już zgłosiłeś ten przepis — czeka na rozpatrzenie.");

        await _recipeReports.AddAsync(new RecipeReport
        {
            RecipeId = recipeId,
            ReportedById = userId,
            Reason = normalized!,
            Status = ReportStatus.Pending,
            CreatedAt = DateTime.UtcNow
        });
        await _recipeReports.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> ReportCommentAsync(int commentId, int userId, string reason)
    {
        var (normalized, error) = NormalizeReason(reason);
        if (error is not null)
            return (false, error);

        if (!await _comments.ExistsAsync(commentId))
            return (false, "Nie znaleziono komentarza.");

        var alreadyReported = await _commentReports.Query()
            .AnyAsync(r => r.CommentId == commentId && r.ReportedById == userId && r.Status == ReportStatus.Pending);
        if (alreadyReported)
            return (false, "Już zgłosiłeś ten komentarz — czeka na rozpatrzenie.");

        await _commentReports.AddAsync(new CommentReport
        {
            CommentId = commentId,
            ReportedById = userId,
            Reason = normalized!,
            Status = ReportStatus.Pending,
            CreatedAt = DateTime.UtcNow
        });
        await _commentReports.SaveChangesAsync();
        return (true, null);
    }

    // --- Moderacja ---

    public async Task<ReportsViewModel> GetReportsAsync(bool includeResolved = false)
    {
        var recipeQuery = _recipeReports.Query()
            .Include(r => r.Recipe)
            .Include(r => r.ReportedBy)
            .Include(r => r.ResolvedBy)
            .AsNoTracking();
        if (!includeResolved)
            recipeQuery = recipeQuery.Where(r => r.Status == ReportStatus.Pending);

        var recipeEntities = await recipeQuery
            .OrderBy(r => r.Status)
            .ThenByDescending(r => r.CreatedAt)
            .ToListAsync();

        var recipeReports = recipeEntities.Select(r => new RecipeReportDto(
            r.Id,
            r.RecipeId,
            r.Recipe.Name,
            r.ReportedBy.PublicUsername,
            r.Reason,
            r.Status,
            r.CreatedAt,
            r.ResolvedBy?.PublicUsername,
            r.ResolvedAt)).ToList();

        var commentQuery = _commentReports.Query()
            .Include(r => r.Comment).ThenInclude(c => c.User)
            .Include(r => r.ReportedBy)
            .Include(r => r.ResolvedBy)
            .AsNoTracking();
        if (!includeResolved)
            commentQuery = commentQuery.Where(r => r.Status == ReportStatus.Pending);

        var commentEntities = await commentQuery
            .OrderBy(r => r.Status)
            .ThenByDescending(r => r.CreatedAt)
            .ToListAsync();

        var commentReports = commentEntities.Select(r => new CommentReportDto(
            r.Id,
            r.CommentId,
            r.Comment.RecipeId,
            r.Comment.Content,
            r.Comment.User.PublicUsername,
            r.ReportedBy.PublicUsername,
            r.Reason,
            r.Status,
            r.CreatedAt,
            r.ResolvedBy?.PublicUsername,
            r.ResolvedAt)).ToList();

        return new ReportsViewModel(recipeReports, commentReports, includeResolved);
    }

    public async Task<int> GetPendingCountAsync()
    {
        var recipes = await _recipeReports.Query().CountAsync(r => r.Status == ReportStatus.Pending);
        var comments = await _commentReports.Query().CountAsync(r => r.Status == ReportStatus.Pending);
        return recipes + comments;
    }

    public async Task<(bool Success, string? Error)> SetRecipeReportStatusAsync(int reportId, int moderatorId, ReportStatus status)
    {
        var report = await _recipeReports.GetByIdAsync(reportId);
        if (report is null)
            return (false, "Nie znaleziono zgłoszenia.");

        report.Status = status;
        report.ResolvedById = moderatorId;
        report.ResolvedAt = DateTime.UtcNow;
        _recipeReports.Update(report);
        await _recipeReports.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> SetCommentReportStatusAsync(int reportId, int moderatorId, ReportStatus status)
    {
        var report = await _commentReports.GetByIdAsync(reportId);
        if (report is null)
            return (false, "Nie znaleziono zgłoszenia.");

        report.Status = status;
        report.ResolvedById = moderatorId;
        report.ResolvedAt = DateTime.UtcNow;
        _commentReports.Update(report);
        await _commentReports.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> DeleteReportedRecipeAsync(int reportId, int moderatorId)
    {
        var report = await _recipeReports.GetByIdAsync(reportId);
        if (report is null)
            return (false, "Nie znaleziono zgłoszenia.");

        // Usunięcie przepisu kaskadowo usuwa też powiązane z nim zgłoszenia.
        return await _recipeService.DeleteAsync(report.RecipeId, moderatorId, isModerator: true);
    }

    public async Task<(bool Success, string? Error)> DeleteReportedCommentAsync(int reportId, int moderatorId)
    {
        var report = await _commentReports.GetByIdAsync(reportId);
        if (report is null)
            return (false, "Nie znaleziono zgłoszenia.");

        // Usunięcie komentarza kaskadowo usuwa też powiązane z nim zgłoszenia.
        return await _recipeService.DeleteCommentAsync(report.CommentId, moderatorId, isModerator: true);
    }

    private static (string? Normalized, string? Error) NormalizeReason(string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
            return (null, "Podaj powód zgłoszenia.");

        var trimmed = reason.Trim();
        if (trimmed.Length > MaxReasonLength)
            return (null, $"Powód jest zbyt długi (maksymalnie {MaxReasonLength} znaków).");

        return (trimmed, null);
    }
}
