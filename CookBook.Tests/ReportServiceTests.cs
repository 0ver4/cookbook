using CookBook.Models;
using CookBook.Repositories;
using CookBook.Services;
using MockQueryable.Moq;
using Moq;

namespace CookBook.Tests;

public class ReportServiceTests
{
    private readonly Mock<IRepository<RecipeReport>> _recipeReports = new();
    private readonly Mock<IRepository<CommentReport>> _commentReports = new();
    private readonly Mock<IRepository<Recipe>> _recipes = new();
    private readonly Mock<IRepository<Comment>> _comments = new();
    private readonly Mock<IRecipeService> _recipeService = new();

    private ReportService CreateService() =>
        new(_recipeReports.Object, _commentReports.Object, _recipes.Object, _comments.Object, _recipeService.Object);

    private void SetupRecipeReportsQuery(params RecipeReport[] data) =>
        _recipeReports.Setup(r => r.Query()).Returns(data.AsQueryable().BuildMock());

    private void SetupCommentReportsQuery(params CommentReport[] data) =>
        _commentReports.Setup(r => r.Query()).Returns(data.AsQueryable().BuildMock());

    // -----------------------------------------------------------------------
    // ReportRecipeAsync — walidacja powodu (NormalizeReason)
    // -----------------------------------------------------------------------

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task ReportRecipeAsync_WhenReasonBlank_Fails(string reason)
    {
        var svc = CreateService();

        var (success, error) = await svc.ReportRecipeAsync(1, 1, reason);

        Assert.False(success);
        Assert.NotNull(error);
        _recipeReports.Verify(r => r.AddAsync(It.IsAny<RecipeReport>()), Times.Never);
    }

    [Fact]
    public async Task ReportRecipeAsync_WhenReasonTooLong_Fails()
    {
        var svc = CreateService();
        var tooLong = new string('x', 1001); // limit to 1000

        var (success, error) = await svc.ReportRecipeAsync(1, 1, tooLong);

        Assert.False(success);
        Assert.NotNull(error);
    }

    [Fact]
    public async Task ReportRecipeAsync_WhenRecipeMissing_Fails()
    {
        _recipes.Setup(r => r.ExistsAsync(5)).ReturnsAsync(false);
        var svc = CreateService();

        var (success, error) = await svc.ReportRecipeAsync(5, 1, "spam");

        Assert.False(success);
        Assert.NotNull(error);
    }

    [Fact]
    public async Task ReportRecipeAsync_WhenAlreadyReportedByUser_Fails()
    {
        _recipes.Setup(r => r.ExistsAsync(5)).ReturnsAsync(true);
        SetupRecipeReportsQuery(new RecipeReport
        {
            Id = 1, RecipeId = 5, ReportedById = 1, Status = ReportStatus.Pending, Reason = "x"
        });
        var svc = CreateService();

        var (success, error) = await svc.ReportRecipeAsync(5, userId: 1, "znowu spam");

        Assert.False(success);
        Assert.NotNull(error);
        _recipeReports.Verify(r => r.AddAsync(It.IsAny<RecipeReport>()), Times.Never);
    }

    [Fact]
    public async Task ReportRecipeAsync_WhenValid_AddsPendingReportTrimmedAndSaves()
    {
        _recipes.Setup(r => r.ExistsAsync(5)).ReturnsAsync(true);
        SetupRecipeReportsQuery(); // brak wcześniejszych zgłoszeń
        RecipeReport? captured = null;
        _recipeReports.Setup(r => r.AddAsync(It.IsAny<RecipeReport>()))
            .Callback<RecipeReport>(r => captured = r).Returns(Task.CompletedTask);
        _recipeReports.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);
        var svc = CreateService();

        var (success, error) = await svc.ReportRecipeAsync(5, 9, "  obraźliwe treści  ");

        Assert.True(success);
        Assert.Null(error);
        Assert.NotNull(captured);
        Assert.Equal(5, captured!.RecipeId);
        Assert.Equal(9, captured.ReportedById);
        Assert.Equal("obraźliwe treści", captured.Reason); // przycięte
        Assert.Equal(ReportStatus.Pending, captured.Status);
        _recipeReports.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    // -----------------------------------------------------------------------
    // ReportCommentAsync
    // -----------------------------------------------------------------------

    [Fact]
    public async Task ReportCommentAsync_WhenCommentMissing_Fails()
    {
        _comments.Setup(r => r.ExistsAsync(7)).ReturnsAsync(false);
        var svc = CreateService();

        var (success, error) = await svc.ReportCommentAsync(7, 1, "spam");

        Assert.False(success);
        Assert.NotNull(error);
    }

    [Fact]
    public async Task ReportCommentAsync_WhenAlreadyReported_Fails()
    {
        _comments.Setup(r => r.ExistsAsync(7)).ReturnsAsync(true);
        SetupCommentReportsQuery(new CommentReport
        {
            Id = 1, CommentId = 7, ReportedById = 1, Status = ReportStatus.Pending, Reason = "x"
        });
        var svc = CreateService();

        var (success, _) = await svc.ReportCommentAsync(7, 1, "spam");

        Assert.False(success);
        _commentReports.Verify(r => r.AddAsync(It.IsAny<CommentReport>()), Times.Never);
    }

    [Fact]
    public async Task ReportCommentAsync_WhenValid_AddsAndSaves()
    {
        _comments.Setup(r => r.ExistsAsync(7)).ReturnsAsync(true);
        SetupCommentReportsQuery();
        _commentReports.Setup(r => r.AddAsync(It.IsAny<CommentReport>())).Returns(Task.CompletedTask);
        _commentReports.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);
        var svc = CreateService();

        var (success, _) = await svc.ReportCommentAsync(7, 9, "spam");

        Assert.True(success);
        _commentReports.Verify(r => r.AddAsync(It.Is<CommentReport>(c =>
            c.CommentId == 7 && c.ReportedById == 9 && c.Status == ReportStatus.Pending)), Times.Once);
        _commentReports.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    // -----------------------------------------------------------------------
    // GetPendingCountAsync
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetPendingCountAsync_SumsPendingRecipeAndCommentReports()
    {
        SetupRecipeReportsQuery(
            new RecipeReport { Id = 1, Status = ReportStatus.Pending, Reason = "x" },
            new RecipeReport { Id = 2, Status = ReportStatus.Pending, Reason = "x" },
            new RecipeReport { Id = 3, Status = ReportStatus.Resolved, Reason = "x" }); // nie liczone
        SetupCommentReportsQuery(
            new CommentReport { Id = 1, Status = ReportStatus.Pending, Reason = "x" },
            new CommentReport { Id = 2, Status = ReportStatus.Dismissed, Reason = "x" }); // nie liczone
        var svc = CreateService();

        var count = await svc.GetPendingCountAsync();

        Assert.Equal(3, count); // 2 przepisy + 1 komentarz
    }

    // -----------------------------------------------------------------------
    // SetRecipeReportStatusAsync / SetCommentReportStatusAsync
    // -----------------------------------------------------------------------

    [Fact]
    public async Task SetRecipeReportStatusAsync_WhenMissing_Fails()
    {
        _recipeReports.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((RecipeReport?)null);
        var svc = CreateService();

        var (success, error) = await svc.SetRecipeReportStatusAsync(1, 99, ReportStatus.Resolved);

        Assert.False(success);
        Assert.NotNull(error);
    }

    [Fact]
    public async Task SetRecipeReportStatusAsync_WhenFound_SetsStatusResolverAndSaves()
    {
        var report = new RecipeReport { Id = 1, Status = ReportStatus.Pending, Reason = "x" };
        _recipeReports.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(report);
        _recipeReports.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);
        var svc = CreateService();

        var (success, _) = await svc.SetRecipeReportStatusAsync(1, moderatorId: 42, ReportStatus.Dismissed);

        Assert.True(success);
        Assert.Equal(ReportStatus.Dismissed, report.Status);
        Assert.Equal(42, report.ResolvedById);
        Assert.NotNull(report.ResolvedAt);
        _recipeReports.Verify(r => r.Update(report), Times.Once);
        _recipeReports.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task SetCommentReportStatusAsync_WhenFound_SetsStatusAndSaves()
    {
        var report = new CommentReport { Id = 1, Status = ReportStatus.Pending, Reason = "x" };
        _commentReports.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(report);
        _commentReports.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);
        var svc = CreateService();

        var (success, _) = await svc.SetCommentReportStatusAsync(1, 42, ReportStatus.Resolved);

        Assert.True(success);
        Assert.Equal(ReportStatus.Resolved, report.Status);
        Assert.Equal(42, report.ResolvedById);
        _commentReports.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    // -----------------------------------------------------------------------
    // DeleteReportedRecipeAsync / DeleteReportedCommentAsync — delegacja
    // -----------------------------------------------------------------------

    [Fact]
    public async Task DeleteReportedRecipeAsync_WhenReportMissing_Fails()
    {
        _recipeReports.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((RecipeReport?)null);
        var svc = CreateService();

        var (success, error) = await svc.DeleteReportedRecipeAsync(1, 42);

        Assert.False(success);
        Assert.NotNull(error);
        _recipeService.Verify(s => s.DeleteAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<bool>()), Times.Never);
    }

    [Fact]
    public async Task DeleteReportedRecipeAsync_WhenFound_DelegatesToRecipeServiceAsModerator()
    {
        var report = new RecipeReport { Id = 1, RecipeId = 77, Reason = "x" };
        _recipeReports.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(report);
        _recipeService.Setup(s => s.DeleteAsync(77, 42, true)).ReturnsAsync((true, (string?)null));
        var svc = CreateService();

        var (success, _) = await svc.DeleteReportedRecipeAsync(1, moderatorId: 42);

        Assert.True(success);
        _recipeService.Verify(s => s.DeleteAsync(77, 42, true), Times.Once);
    }

    [Fact]
    public async Task DeleteReportedCommentAsync_WhenFound_DelegatesToRecipeServiceAsModerator()
    {
        var report = new CommentReport { Id = 1, CommentId = 88, Reason = "x" };
        _commentReports.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(report);
        _recipeService.Setup(s => s.DeleteCommentAsync(88, 42, true)).ReturnsAsync((true, (string?)null));
        var svc = CreateService();

        var (success, _) = await svc.DeleteReportedCommentAsync(1, 42);

        Assert.True(success);
        _recipeService.Verify(s => s.DeleteCommentAsync(88, 42, true), Times.Once);
    }
}
