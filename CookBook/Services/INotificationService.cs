namespace CookBook.Services;

public record NotificationDto(
    int Id,
    string TypeName,
    string? TriggeredByName,
    string? RecipeName,
    int RecipeId,
    int CommentId,
    bool IsRead,
    DateTime CreatedAt);

public interface INotificationService
{
    Task<IReadOnlyList<NotificationDto>> GetForUserAsync(int userId);
    Task<int> GetUnreadCountAsync(int userId);
    Task MarkAsReadAsync(int notificationId, int userId);
    Task MarkAllAsReadAsync(int userId);
}
