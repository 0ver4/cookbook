using CookBook.Models;
using CookBook.Repositories;
using CookBook.Services;
using MockQueryable.Moq;
using Moq;

namespace CookBook.Tests;

public class NotificationServiceTests
{
    // Helper: tworzy serwis + mock repozytorium
    private static (NotificationService svc, Mock<IRepository<Notification>> repoMock) Create()
    {
        var repo = new Mock<IRepository<Notification>>();
        var svc = new NotificationService(repo.Object);
        return (svc, repo);
    }

    // Helper: konfiguruje repo.Query() tak żeby zwracało podaną listę
    // BuildMock() z MockQueryable.Moq dodaje obsługę ToListAsync, CountAsync, itd.
    private static void SetupQuery(Mock<IRepository<Notification>> repo, List<Notification> data)
    {
        var mock = data.AsQueryable().BuildMock();
        repo.Setup(r => r.Query()).Returns(mock);
    }

    // -----------------------------------------------------------------------
    // CreateAsync
    // -----------------------------------------------------------------------

    [Fact]
    public async Task CreateAsync_CallsAddAsyncWithCorrectNotification()
    {
        // Arrange
        var (svc, repo) = Create();

        Notification? captured = null;
        repo.Setup(r => r.AddAsync(It.IsAny<Notification>()))
            .Callback<Notification>(n => captured = n)
            .Returns(Task.CompletedTask);
        repo.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);

        // Act
        await svc.CreateAsync(userId: 5, typeId: 2, triggeredByUserId: 7, commentId: 3);

        // Assert
        Assert.NotNull(captured);
        Assert.Equal(5, captured.UserId);
        Assert.Equal(2, captured.NotificationTypeId);
        Assert.Equal(7, captured.TriggeredByUserId);
        Assert.Equal(3, captured.CommentId);
    }

    [Fact]
    public async Task CreateAsync_CallsSaveChangesAsync()
    {
        // Arrange
        var (svc, repo) = Create();
        repo.Setup(r => r.AddAsync(It.IsAny<Notification>())).Returns(Task.CompletedTask);
        repo.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);

        // Act
        await svc.CreateAsync(userId: 1, typeId: 1, triggeredByUserId: null, commentId: 1);

        // Assert
        repo.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WithNullOptionals_SetsNulls()
    {
        // Arrange
        var (svc, repo) = Create();

        Notification? captured = null;
        repo.Setup(r => r.AddAsync(It.IsAny<Notification>()))
            .Callback<Notification>(n => captured = n)
            .Returns(Task.CompletedTask);
        repo.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);

        // Act
        await svc.CreateAsync(userId: 1, typeId: 1, triggeredByUserId: null, commentId: 1);

        // Assert
        Assert.Null(captured!.TriggeredByUserId);
    }

    // -----------------------------------------------------------------------
    // GetUnreadCountAsync
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetUnreadCountAsync_ReturnsOnlyUnreadForUser()
    {
        // Arrange
        var (svc, repo) = Create();
        var data = new List<Notification>
        {
            new() { Id = 1, UserId = 1, IsRead = false },  // liczymy
            new() { Id = 2, UserId = 1, IsRead = false },  // liczymy
            new() { Id = 3, UserId = 1, IsRead = true  },  // przeczytane - nie liczymy
            new() { Id = 4, UserId = 2, IsRead = false },  // inny user - nie liczymy
        };
        SetupQuery(repo, data);

        // Act
        var count = await svc.GetUnreadCountAsync(userId: 1);

        // Assert
        Assert.Equal(2, count);
    }

    [Fact]
    public async Task GetUnreadCountAsync_WhenNoneUnread_ReturnsZero()
    {
        // Arrange
        var (svc, repo) = Create();
        var data = new List<Notification>
        {
            new() { Id = 1, UserId = 1, IsRead = true },
        };
        SetupQuery(repo, data);

        // Act
        var count = await svc.GetUnreadCountAsync(userId: 1);

        // Assert
        Assert.Equal(0, count);
    }

    // -----------------------------------------------------------------------
    // GetForUserAsync
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetForUserAsync_ReturnsOnlyNotificationsForUser()
    {
        // Arrange
        var (svc, repo) = Create();
        var data = new List<Notification>
        {
            new() { Id = 1, UserId = 1, NotificationType = new NotificationType { Name = "NewComment" }, Comment = new Comment(), CreatedAt = DateTime.UtcNow },
            new() { Id = 2, UserId = 2, NotificationType = new NotificationType { Name = "NewComment" }, Comment = new Comment(), CreatedAt = DateTime.UtcNow },
        };
        SetupQuery(repo, data);

        // Act
        var result = await svc.GetForUserAsync(userId: 1);

        // Assert
        Assert.Single(result);
        Assert.Equal(1, result[0].Id);
    }

    [Fact]
    public async Task GetForUserAsync_MapsFieldsCorrectly()
    {
        // Arrange
        var (svc, repo) = Create();
        var createdAt = new DateTime(2026, 1, 15, 12, 0, 0, DateTimeKind.Utc);
        var data = new List<Notification>
        {
            new()
            {
                Id = 42,
                UserId = 1,
                IsRead = false,
                CreatedAt = createdAt,
                NotificationType = new NotificationType { Name = "Reply" },
                TriggeredByUser = new ApplicationUser { UserName = "jan_kowalski" },
                Comment = new Comment { RecipeId = 5, Recipe = new Recipe { Id = 5, Name = "Bigos" } }
            }
        };
        SetupQuery(repo, data);

        // Act
        var result = await svc.GetForUserAsync(userId: 1);

        // Assert
        var dto = result[0];
        Assert.Equal(42, dto.Id);
        Assert.Equal("Reply", dto.TypeName);
        Assert.Equal("jan_kowalski", dto.TriggeredByName);
        Assert.Equal("Bigos", dto.RecipeName);
        Assert.Equal(5, dto.RecipeId);
        Assert.False(dto.IsRead);
        Assert.Equal(createdAt, dto.CreatedAt);
    }

    [Fact]
    public async Task GetForUserAsync_WhenNoTriggeredByUser_TriggeredByNameIsNull()
    {
        // Arrange
        var (svc, repo) = Create();
        var data = new List<Notification>
        {
            new()
            {
                Id = 1, UserId = 1,
                NotificationType = new NotificationType { Name = "NewComment" },
                TriggeredByUser = null,
                Comment = new Comment(),
                CreatedAt = DateTime.UtcNow
            }
        };
        SetupQuery(repo, data);

        // Act
        var result = await svc.GetForUserAsync(userId: 1);

        // Assert
        Assert.Null(result[0].TriggeredByName);
    }

    [Fact]
    public async Task GetForUserAsync_ReturnsOrderedByCreatedAtDescending()
    {
        // Arrange
        var (svc, repo) = Create();
        var data = new List<Notification>
        {
            new() { Id = 1, UserId = 1, NotificationType = new NotificationType { Name = "A" }, Comment = new Comment(), CreatedAt = new DateTime(2026, 1, 1) },
            new() { Id = 2, UserId = 1, NotificationType = new NotificationType { Name = "B" }, Comment = new Comment(), CreatedAt = new DateTime(2026, 3, 1) },
            new() { Id = 3, UserId = 1, NotificationType = new NotificationType { Name = "C" }, Comment = new Comment(), CreatedAt = new DateTime(2026, 2, 1) },
        };
        SetupQuery(repo, data);

        // Act
        var result = await svc.GetForUserAsync(userId: 1);

        // Assert
        Assert.Equal(2, result[0].Id); // najnowszy
        Assert.Equal(3, result[1].Id);
        Assert.Equal(1, result[2].Id); // najstarszy
    }

    // -----------------------------------------------------------------------
    // MarkAsReadAsync
    // -----------------------------------------------------------------------

    [Fact]
    public async Task MarkAsReadAsync_SetsIsReadAndSaves()
    {
        // Arrange
        var (svc, repo) = Create();
        var notification = new Notification { Id = 1, UserId = 1, IsRead = false };
        SetupQuery(repo, [notification]);
        repo.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);

        // Act
        await svc.MarkAsReadAsync(notificationId: 1, userId: 1);

        // Assert
        Assert.True(notification.IsRead);
        repo.Verify(r => r.Update(notification), Times.Once);
        repo.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task MarkAsReadAsync_WhenNotificationNotFound_DoesNothing()
    {
        // Arrange
        var (svc, repo) = Create();
        // Baza jest pusta
        SetupQuery(repo, []);

        // Act
        await svc.MarkAsReadAsync(notificationId: 99, userId: 1);

        // Assert - Update i SaveChanges nie mogą być wołane
        repo.Verify(r => r.Update(It.IsAny<Notification>()), Times.Never);
        repo.Verify(r => r.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task MarkAsReadAsync_WhenWrongUser_DoesNothing()
    {
        // Arrange
        var (svc, repo) = Create();
        // Powiadomienie należy do usera 2, ale prosimy o odczyt jako user 1
        var notification = new Notification { Id = 1, UserId = 2, IsRead = false };
        SetupQuery(repo, [notification]);

        // Act
        await svc.MarkAsReadAsync(notificationId: 1, userId: 1);

        // Assert
        repo.Verify(r => r.Update(It.IsAny<Notification>()), Times.Never);
    }

    // -----------------------------------------------------------------------
    // MarkAllAsReadAsync
    // -----------------------------------------------------------------------

    [Fact]
    public async Task MarkAllAsReadAsync_MarksAllUnreadForUser()
    {
        // Arrange
        var (svc, repo) = Create();
        var n1 = new Notification { Id = 1, UserId = 1, IsRead = false };
        var n2 = new Notification { Id = 2, UserId = 1, IsRead = false };
        var n3 = new Notification { Id = 3, UserId = 1, IsRead = true };  // już przeczytane
        var n4 = new Notification { Id = 4, UserId = 2, IsRead = false }; // inny user
        SetupQuery(repo, [n1, n2, n3, n4]);
        repo.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);

        // Act
        await svc.MarkAllAsReadAsync(userId: 1);

        // Assert
        Assert.True(n1.IsRead);
        Assert.True(n2.IsRead);
        Assert.True(n3.IsRead); // już było true, bez zmian
        Assert.False(n4.IsRead); // inny user - nie ruszamy
    }

    [Fact]
    public async Task MarkAllAsReadAsync_CallsUpdateForEachUnread()
    {
        // Arrange
        var (svc, repo) = Create();
        var n1 = new Notification { Id = 1, UserId = 1, IsRead = false };
        var n2 = new Notification { Id = 2, UserId = 1, IsRead = false };
        SetupQuery(repo, [n1, n2]);
        repo.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);

        // Act
        await svc.MarkAllAsReadAsync(userId: 1);

        // Assert - Update wołany 2 razy (raz dla każdego nieprzeczytanego)
        repo.Verify(r => r.Update(It.IsAny<Notification>()), Times.Exactly(2));
        repo.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task MarkAllAsReadAsync_WhenNoUnread_SavesChangesWithoutUpdate()
    {
        // Arrange
        var (svc, repo) = Create();
        SetupQuery(repo, []); // brak powiadomień
        repo.Setup(r => r.SaveChangesAsync()).ReturnsAsync(0);

        // Act
        await svc.MarkAllAsReadAsync(userId: 1);

        // Assert
        repo.Verify(r => r.Update(It.IsAny<Notification>()), Times.Never);
        repo.Verify(r => r.SaveChangesAsync(), Times.Once);
    }
}
