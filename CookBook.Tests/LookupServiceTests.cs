using CookBook.Models;
using CookBook.Repositories;
using CookBook.Services;
using MockQueryable.Moq;
using Moq;

namespace CookBook.Tests;

/// <summary>
/// Testy generycznego LookupService na przykładzie słownika Unit (Id, Name).
/// Sprawdzanie unikalności nazwy wydzielono do ILookupRepository.ExistsByNameAsync,
/// dzięki czemu cała logika serwisu — łącznie z happy-path CreateAsync/UpdateAsync — jest mockowalna.
/// </summary>
public class LookupServiceTests
{
    private readonly Mock<ILookupRepository<Unit>> _repo = new();

    // Zwracamy przez ILookupOps, bo to interfejs nadaje nazwy polom krotek (Id, Name).
    private ILookupOps CreateService() => new LookupService<Unit>(_repo.Object);

    // -----------------------------------------------------------------------
    // GetAllAsync
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetAllAsync_OrdersByNameAndMaps()
    {
        _repo.Setup(r => r.GetAllAsync()).ReturnsAsync(new[]
        {
            new Unit { Id = 1, Name = "litr" },
            new Unit { Id = 2, Name = "gram" },
            new Unit { Id = 3, Name = "sztuka" },
        });
        var svc = CreateService();

        var result = await svc.GetAllAsync();

        Assert.Equal(new[] { "gram", "litr", "sztuka" }, result.Select(x => x.Name));
        Assert.Equal(2, result[0].Id); // "gram" ma Id 2
    }

    [Fact]
    public async Task GetAllAsync_WhenEmpty_ReturnsEmpty()
    {
        _repo.Setup(r => r.GetAllAsync()).ReturnsAsync(Array.Empty<Unit>());
        var svc = CreateService();

        Assert.Empty(await svc.GetAllAsync());
    }

    // -----------------------------------------------------------------------
    // GetByIdAsync
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetByIdAsync_WhenExists_ReturnsTuple()
    {
        _repo.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(new Unit { Id = 2, Name = "gram" });
        var svc = CreateService();

        var result = await svc.GetByIdAsync(2);

        Assert.NotNull(result);
        Assert.Equal(2, result!.Value.Id);
        Assert.Equal("gram", result.Value.Name);
    }

    [Fact]
    public async Task GetByIdAsync_WhenMissing_ReturnsNull()
    {
        _repo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Unit?)null);
        var svc = CreateService();

        Assert.Null(await svc.GetByIdAsync(99));
    }

    // -----------------------------------------------------------------------
    // CountAsync
    // -----------------------------------------------------------------------

    [Fact]
    public async Task CountAsync_ReturnsNumberOfEntries()
    {
        _repo.Setup(r => r.Query()).Returns(new[]
        {
            new Unit { Id = 1, Name = "a" },
            new Unit { Id = 2, Name = "b" },
        }.AsQueryable().BuildMock());
        var svc = CreateService();

        Assert.Equal(2, await svc.CountAsync());
    }

    // -----------------------------------------------------------------------
    // DeleteAsync
    // -----------------------------------------------------------------------

    [Fact]
    public async Task DeleteAsync_WhenExists_RemovesAndSaves()
    {
        var unit = new Unit { Id = 1, Name = "gram" };
        _repo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(unit);
        _repo.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);
        var svc = CreateService();

        await svc.DeleteAsync(1);

        _repo.Verify(r => r.Remove(unit), Times.Once);
        _repo.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WhenMissing_DoesNothing()
    {
        _repo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Unit?)null);
        var svc = CreateService();

        await svc.DeleteAsync(99);

        _repo.Verify(r => r.Remove(It.IsAny<Unit>()), Times.Never);
        _repo.Verify(r => r.SaveChangesAsync(), Times.Never);
    }

    // -----------------------------------------------------------------------
    // CreateAsync — walidacja nazwy (przed zapytaniem o unikalność)
    // -----------------------------------------------------------------------

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CreateAsync_WhenNameBlank_Fails(string name)
    {
        var svc = CreateService();

        var (success, error) = await svc.CreateAsync(name);

        Assert.False(success);
        Assert.NotNull(error);
        _repo.Verify(r => r.AddAsync(It.IsAny<Unit>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WhenNameTooLong_Fails()
    {
        var svc = CreateService();

        var (success, error) = await svc.CreateAsync(new string('x', 51)); // limit 50

        Assert.False(success);
        Assert.NotNull(error);
    }

    [Fact]
    public async Task CreateAsync_WhenNameAlreadyExists_Fails()
    {
        _repo.Setup(r => r.ExistsByNameAsync("gram", 0)).ReturnsAsync(true);
        var svc = CreateService();

        var (success, error) = await svc.CreateAsync("gram");

        Assert.False(success);
        Assert.NotNull(error);
        _repo.Verify(r => r.AddAsync(It.IsAny<Unit>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WhenValid_AddsTrimmedEntityAndSaves()
    {
        _repo.Setup(r => r.ExistsByNameAsync(It.IsAny<string>(), It.IsAny<int>())).ReturnsAsync(false);
        Unit? captured = null;
        _repo.Setup(r => r.AddAsync(It.IsAny<Unit>())).Callback<Unit>(u => captured = u).Returns(Task.CompletedTask);
        _repo.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);
        var svc = CreateService();

        var (success, error) = await svc.CreateAsync("  dag  ");

        Assert.True(success);
        Assert.Null(error);
        Assert.Equal("dag", captured!.Name); // przycięte
        _repo.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    // -----------------------------------------------------------------------
    // UpdateAsync — gałęzie przed zapytaniem o unikalność
    // -----------------------------------------------------------------------

    [Fact]
    public async Task UpdateAsync_WhenEntityMissing_Fails()
    {
        _repo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((Unit?)null);
        var svc = CreateService();

        var (success, error) = await svc.UpdateAsync(1, "gram");

        Assert.False(success);
        Assert.NotNull(error);
        _repo.Verify(r => r.Update(It.IsAny<Unit>()), Times.Never);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task UpdateAsync_WhenNameBlank_Fails(string name)
    {
        _repo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Unit { Id = 1, Name = "gram" });
        var svc = CreateService();

        var (success, error) = await svc.UpdateAsync(1, name);

        Assert.False(success);
        Assert.NotNull(error);
        _repo.Verify(r => r.Update(It.IsAny<Unit>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_WhenNameTakenByAnother_Fails()
    {
        _repo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Unit { Id = 1, Name = "gram" });
        // Inny wpis (Id != 1) ma już nazwę "litr"
        _repo.Setup(r => r.ExistsByNameAsync("litr", 1)).ReturnsAsync(true);
        var svc = CreateService();

        var (success, error) = await svc.UpdateAsync(1, "litr");

        Assert.False(success);
        Assert.NotNull(error);
        _repo.Verify(r => r.Update(It.IsAny<Unit>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_WhenValid_UpdatesNameAndSaves()
    {
        var entity = new Unit { Id = 1, Name = "gram" };
        _repo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(entity);
        // wykluczamy własne Id przy sprawdzaniu unikalności -> nazwa wolna
        _repo.Setup(r => r.ExistsByNameAsync("dekagram", 1)).ReturnsAsync(false);
        _repo.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);
        var svc = CreateService();

        var (success, error) = await svc.UpdateAsync(1, "  dekagram  ");

        Assert.True(success);
        Assert.Null(error);
        Assert.Equal("dekagram", entity.Name); // zaktualizowana i przycięta
        _repo.Verify(r => r.Update(entity), Times.Once);
        _repo.Verify(r => r.SaveChangesAsync(), Times.Once);
    }
}
