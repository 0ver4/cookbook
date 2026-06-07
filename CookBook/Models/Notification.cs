namespace CookBook.Models;

public class Notification
{
    public int Id { get; set; }

    public int UserId { get; set; }
    public ApplicationUser User { get; set; } = null!;

    public int NotificationTypeId { get; set; }
    public NotificationType NotificationType { get; set; } = null!;

    public int? TriggeredByUserId { get; set; }
    public ApplicationUser? TriggeredByUser { get; set; }

    public int? RecipeId { get; set; }
    public Recipe? Recipe { get; set; }

    public int? CommentId { get; set; }
    public Comment? Comment { get; set; }

    public bool IsRead { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
