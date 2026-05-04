using System.Net;
using System.Net.Http.Json;
using Xunit;
using UTB.Minute.Contracts.MenuItems;
using Microsoft.EntityFrameworkCore;

namespace UTB.Minute.WebApi.Tests;

/// <summary>
/// Integration tests for Menu Items API endpoints
/// Tests HTTP API operations for menu management
/// </summary>
[Collection("Database collection")]
public class MenuItemApiTests : IAsyncLifetime
{
    private readonly TestFixture _fixture;

    public MenuItemApiTests(TestFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        await _fixture.ResetDatabaseAsync();
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Test: GET /menu-items returns all menu items
    /// Requirement: Vytvoření a čtení položek menu a jejich testy
    /// </summary>
    [Fact]
    public async Task GetMenuItems_ReturnsOkWithMenuItems()
    {
        // Act
        var response = await _fixture.HttpClient.GetAsync("/menu-items", CancellationToken.None);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var menuItems = await response.Content.ReadFromJsonAsync<List<MenuItemDto>>(cancellationToken: CancellationToken.None);

        Assert.NotNull(menuItems);
        Assert.Equal(3, menuItems.Count);
    }

    /// <summary>
    /// Test: GET /menu-items/today returns today's menu items
    /// </summary>
    [Fact]
    public async Task GetTodayMenuItems_ReturnsOkWithTodayMenu()
    {
        // Act
        var response = await _fixture.HttpClient.GetAsync("/menu-items/today", CancellationToken.None);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var menuItems = await response.Content.ReadFromJsonAsync<List<MenuItemDto>>(cancellationToken: CancellationToken.None);

        Assert.NotNull(menuItems);
        Assert.Equal(2, menuItems.Count);
    }

    /// <summary>
    /// Test: GET /menu-items/{id} returns specific menu item
    /// </summary>
    [Fact]
    public async Task GetMenuItemById_WithValidId_ReturnsOkWithMenuItem()
    {
        // Arrange
        using var context = _fixture.CreateContext();
        var menuItem = await context.MenuItems.FirstAsync(CancellationToken.None);

        // Act
        var response = await _fixture.HttpClient.GetAsync($"/menu-items/{menuItem.Id}", CancellationToken.None);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var menuItemDto = await response.Content.ReadFromJsonAsync<MenuItemDto>(cancellationToken: CancellationToken.None);

        Assert.NotNull(menuItemDto);
        Assert.Equal(menuItem.Id, menuItemDto.Id);
        Assert.Equal(menuItem.MealId, menuItemDto.MealId);
        Assert.Equal(menuItem.AvailablePortions, menuItemDto.AvailablePortions);
    }

    /// <summary>
    /// Test: GET /menu-items/{id} returns NotFound for invalid ID
    /// </summary>
    [Fact]
    public async Task GetMenuItemById_WithInvalidId_ReturnsNotFound()
    {
        // Act
        var response = await _fixture.HttpClient.GetAsync("/menu-items/999", CancellationToken.None);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    /// <summary>
    /// Test: POST /menu-items creates new menu item and persists it
    /// Verifies: correct HTTP status (201 Created), DTO in response, Location header, and database persistence
    /// Requirement: Vytvoření a čtení položek menu a jejich testy
    /// </summary>
    [Fact]
    public async Task CreateMenuItem_ReturnsCreatedAndPersistsMenuItem()
    {
        // Arrange
        using var context = _fixture.CreateContext();
        var meal = await context.Meals.FirstAsync(CancellationToken.None);
        var tomorrow = DateOnly.FromDateTime(DateTime.Today.AddDays(1));

        var createMenuItemDto = new CreateMenuItemDto(
            tomorrow,
            meal.Id,
            20);

        // Act
        var response = await _fixture.HttpClient.PostAsJsonAsync(
            "/menu-items",
            createMenuItemDto,
            CancellationToken.None);

        // Assert - HTTP response
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var menuItemDto = await response.Content.ReadFromJsonAsync<MenuItemDto>(cancellationToken: CancellationToken.None);

        Assert.NotNull(menuItemDto);
        Assert.Equal(createMenuItemDto.Date, menuItemDto.Date);
        Assert.Equal(createMenuItemDto.MealId, menuItemDto.MealId);
        Assert.Equal(createMenuItemDto.AvailablePortions, menuItemDto.AvailablePortions);

        // Assert - Location header
        Assert.NotNull(response.Headers.Location);
        Assert.EndsWith($"/menu-items/{menuItemDto.Id}", response.Headers.Location.ToString());

        // Assert - Database persistence
        using var contextAfterCreate = _fixture.CreateContext();
        var menuItemFromDb = await contextAfterCreate.MenuItems.FindAsync(menuItemDto.Id, CancellationToken.None);

        Assert.NotNull(menuItemFromDb);
        Assert.Equal(createMenuItemDto.Date, menuItemFromDb.Date);
        Assert.Equal(createMenuItemDto.MealId, menuItemFromDb.MealId);
        Assert.Equal(createMenuItemDto.AvailablePortions, menuItemFromDb.AvailablePortions);
    }

    /// <summary>
    /// Test: POST /menu-items returns BadRequest when meal does not exist
    /// </summary>
    [Fact]
    public async Task CreateMenuItem_WithNonExistentMeal_ReturnsBadRequest()
    {
        // Arrange
        var tomorrow = DateOnly.FromDateTime(DateTime.Today.AddDays(1));
        var createMenuItemDto = new CreateMenuItemDto(tomorrow, 999, 10);

        // Act
        var response = await _fixture.HttpClient.PostAsJsonAsync(
            "/menu-items",
            createMenuItemDto,
            CancellationToken.None);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>
    /// Test: PUT /menu-items/{id} updates existing menu item
    /// Verifies: correct HTTP status (204 NoContent) and updated data in database
    /// Requirement: Úprava a smazání položek menu a jejich testy
    /// </summary>
    [Fact]
    public async Task UpdateMenuItem_WithValidData_ReturnsNoContentAndUpdatesMenuItem()
    {
        // Arrange
        using var context = _fixture.CreateContext();
        var menuItem = await context.MenuItems.FirstAsync(CancellationToken.None);
        var meal = await context.Meals.Skip(1).FirstAsync(CancellationToken.None);
        var menuItemId = menuItem.Id;
        var newDate = DateOnly.FromDateTime(DateTime.Today.AddDays(2));

        var updateMenuItemDto = new UpdateMenuItemDto(
            newDate,
            meal.Id,
            25);

        // Act
        var response = await _fixture.HttpClient.PutAsJsonAsync(
            $"/menu-items/{menuItemId}",
            updateMenuItemDto,
            CancellationToken.None);

        // Assert - HTTP response
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        // Assert - Database persistence
        using var contextAfterUpdate = _fixture.CreateContext();
        var updatedMenuItem = await contextAfterUpdate.MenuItems.FindAsync(menuItemId, CancellationToken.None);

        Assert.NotNull(updatedMenuItem);
        Assert.Equal(updateMenuItemDto.Date, updatedMenuItem.Date);
        Assert.Equal(updateMenuItemDto.MealId, updatedMenuItem.MealId);
        Assert.Equal(updateMenuItemDto.AvailablePortions, updatedMenuItem.AvailablePortions);
    }

    /// <summary>
    /// Test: PUT /menu-items/{id} returns NotFound for invalid ID
    /// </summary>
    [Fact]
    public async Task UpdateMenuItem_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        var updateMenuItemDto = new UpdateMenuItemDto(DateOnly.FromDateTime(DateTime.Today), 1, 10);

        // Act
        var response = await _fixture.HttpClient.PutAsJsonAsync(
            "/menu-items/999",
            updateMenuItemDto,
            CancellationToken.None);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    /// <summary>
    /// Test: DELETE /menu-items/{id} removes menu item
    /// Verifies: correct HTTP status (204 NoContent) and removal from database
    /// Requirement: Úprava a smazání položek menu a jejich testy
    /// </summary>
    [Fact]
    public async Task DeleteMenuItem_WithValidId_ReturnsNoContentAndDeletesMenuItem()
    {
        // Arrange
        using var context = _fixture.CreateContext();
        var menuItem = await context.MenuItems.FirstAsync(CancellationToken.None);
        var menuItemId = menuItem.Id;

        // Act
        var response = await _fixture.HttpClient.DeleteAsync(
            $"/menu-items/{menuItemId}",
            CancellationToken.None);

        // Assert - HTTP response
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        // Assert - Database removal
        using var contextAfterDelete = _fixture.CreateContext();
        var deletedMenuItem = await contextAfterDelete.MenuItems.FindAsync(menuItemId, CancellationToken.None);

        Assert.Null(deletedMenuItem);
    }

    /// <summary>
    /// Test: DELETE /menu-items/{id} returns NotFound for invalid ID
    /// </summary>
    [Fact]
    public async Task DeleteMenuItem_WithInvalidId_ReturnsNotFound()
    {
        // Act
        var response = await _fixture.HttpClient.DeleteAsync("/menu-items/999", CancellationToken.None);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
