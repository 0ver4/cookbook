using CookBook.Controllers;
using CookBook.Dtos;
using CookBook.Models;
using CookBook.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;
using System.Security.Claims;

namespace CookBook.Tests;

public class CollectionControllerTests
{
    private readonly Mock<ICollectionService> _serviceMock;
    private readonly CollectionController _controller;

    public CollectionControllerTests()
    {
        _serviceMock = new Mock<ICollectionService>();

        var userStoreMock = new Mock<IUserStore<ApplicationUser>>();
        var userManagerMock = new Mock<UserManager<ApplicationUser>>(
            userStoreMock.Object, null, null, null, null, null, null, null, null);

        userManagerMock
            .Setup(u => u.GetUserId(It.IsAny<ClaimsPrincipal>()))
            .Returns("1");

        _controller = new CollectionController(
            _serviceMock.Object,
            userManagerMock.Object);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, "1")
                }, "mock"))
            }
        };

        _controller.TempData = new TempDataDictionary(
            _controller.ControllerContext.HttpContext,
            Mock.Of<ITempDataProvider>());
    }

    // TEST 1: Index zwraca widok z listą kolekcji
    [Fact]
    public async Task Index_ReturnsViewWithCollections()
    {
        // Arrange
        var fakeCollections = new List<CollectionSummaryDto>
        {
            new CollectionSummaryDto(1, "Ulubione", DateTime.Now, 3),
            new CollectionSummaryDto(2, "Do wypróbowania", DateTime.Now, 0)
        };

        _serviceMock
            .Setup(s => s.GetForUserAsync(1))
            .ReturnsAsync(fakeCollections);

        // Act
        var result = await _controller.Index();

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsAssignableFrom<IReadOnlyList<CollectionSummaryDto>>(viewResult.Model);
        Assert.Equal(2, model.Count);
    }

    // TEST 2: Details zwraca NotFound gdy kolekcja nie istnieje
    [Fact]
    public async Task Details_ReturnsNotFound_WhenCollectionDoesNotExist()
    {
        // Arrange
        _serviceMock
            .Setup(s => s.GetDetailsAsync(99, 1))
            .ReturnsAsync((CollectionDetailsDto?)null);

        // Act
        var result = await _controller.Details(99);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    // TEST 3: Details zwraca widok gdy kolekcja istnieje
    [Fact]
    public async Task Details_ReturnsView_WhenCollectionExists()
    {
        // Arrange
        var fakeCollection = new CollectionDetailsDto(
            1, "Ulubione", DateTime.Now,
            new List<CollectionRecipeDto>());

        _serviceMock
            .Setup(s => s.GetDetailsAsync(1, 1))
            .ReturnsAsync(fakeCollection);

        // Act
        var result = await _controller.Details(1);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<CollectionDetailsDto>(viewResult.Model);
        Assert.Equal("Ulubione", model.Name);
    }

    // TEST 4: Create przekierowuje do Details po sukcesie
    [Fact]
    public async Task Create_RedirectsToDetails_WhenSuccessful()
    {
        // Arrange
        _serviceMock
            .Setup(s => s.CreateAsync(1, "Nowa kolekcja"))
            .ReturnsAsync((true, (string?)null, 5));

        // Act
        var result = await _controller.Create("Nowa kolekcja");

        // Assert
        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Details", redirect.ActionName);
        Assert.Equal(5, redirect.RouteValues!["id"]);
    }

    // TEST 5: Create przekierowuje do Index gdy błąd
    [Fact]
    public async Task Create_RedirectsToIndex_WhenFailed()
    {
        // Arrange
        _serviceMock
            .Setup(s => s.CreateAsync(1, ""))
            .ReturnsAsync((false, "Nazwa nie może być pusta", 0));

        // Act
        var result = await _controller.Create("");

        // Assert
        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
    }

    // TEST 6: Delete przekierowuje do Index po sukcesie
    [Fact]
    public async Task Delete_RedirectsToIndex_WhenSuccessful()
    {
        // Arrange
        _serviceMock
            .Setup(s => s.DeleteAsync(1, 1))
            .ReturnsAsync((true, (string?)null));

        // Act
        var result = await _controller.DeleteConfirmed(1);

        // Assert
        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
    }

    // TEST 7: Rename przekierowuje do Details
    [Fact]
    public async Task Rename_RedirectsToDetails()
    {
        // Arrange
        _serviceMock
            .Setup(s => s.RenameAsync(1, 1, "Nowa nazwa"))
            .ReturnsAsync((true, (string?)null));

        // Act
        var result = await _controller.Rename(1, "Nowa nazwa");

        // Assert
        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Details", redirect.ActionName);
    }
}