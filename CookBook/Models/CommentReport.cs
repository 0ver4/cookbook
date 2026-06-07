using System.ComponentModel.DataAnnotations;

namespace CookBook.Models;

public class CommentReport
{
    public int Id { get; set; }

    public int ReportedById { get; set; }
    public ApplicationUser ReportedBy { get; set; } = null!;

    public int CommentId { get; set; }
    public Comment Comment { get; set; } = null!;

    [Required, MaxLength(1000)]
    public string Reason { get; set; } = null!;

    public ReportStatus Status { get; set; } = ReportStatus.Pending;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public int? ResolvedById { get; set; }
    public ApplicationUser? ResolvedBy { get; set; }
    public DateTime? ResolvedAt { get; set; }
}
