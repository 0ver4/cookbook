using CookBook.Models;
using CookBook.Repositories;
using Microsoft.EntityFrameworkCore;

namespace CookBook.Services;

public class NotificationService(IRepository<Notification> repo) : INotificationService
{
    public async Task CreateAsync(int userId, int typeId, int? triggeredByUserId = null, int? recipeId = null, int? commentId = null)
    {
        await repo.AddAsync(new Notification
        {
            UserId = userId,
            NotificationTypeId = typeId,
            TriggeredByUserId = triggeredByUserId,
            RecipeId = recipeId,
            CommentId = commentId
        });
        await repo.SaveChangesAsync();
    }

    public async Task<IReadOnlyList<NotificationDto>> GetForUserAsync(int userId)
    {
        // Najpierw pobieramy encje z bazy (AsEnumerable), potem mapujemy w pamięci
        // żeby móc wywołać metodę AuthorName (LINQ-to-SQL nie obsługuje własnych metod C#)
        var entities = await repo.Query()
            .Where(n => n.UserId == userId)
            .Include(n => n.NotificationType)
            .Include(n => n.TriggeredByUser)
            .Include(n => n.Recipe)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync();

        return entities.Select(n => new NotificationDto(
            n.Id,
            n.NotificationType.Name,
            n.TriggeredByUser != null ? AuthorName(n.TriggeredByUser) : null,
            n.Recipe?.Name,
            n.RecipeId,
            n.IsRead,
            n.CreatedAt)).ToList();
    }

    private static string AuthorName(ApplicationUser user)
    {
        var fullName = $"{user.FirstName} {user.LastName}".Trim();
        return !string.IsNullOrWhiteSpace(fullName)
            ? fullName
            : (user.UserName ?? user.Email ?? "Użytkownik");
    }

    public async Task<int> GetUnreadCountAsync(int userId)
    {
        return await repo.Query()
            .CountAsync(n => n.UserId == userId && !n.IsRead);
    }

    public async Task MarkAsReadAsync(int notificationId, int userId)
    {
        var n = await repo.Query()
            .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId);
        if (n is null) return;
        n.IsRead = true;
        repo.Update(n);
        await repo.SaveChangesAsync();
    }

    public async Task MarkAllAsReadAsync(int userId)
    {
        var unread = await repo.Query()
            .Where(n => n.UserId == userId && !n.IsRead)
            .ToListAsync();
        foreach (var n in unread)
        {
            n.IsRead = true;
            repo.Update(n);
        }
        await repo.SaveChangesAsync();
    }
}
