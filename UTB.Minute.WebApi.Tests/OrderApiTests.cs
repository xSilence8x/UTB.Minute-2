using System.Net;
using System.Net.Http.Json;
using Xunit;
using UTB.Minute.Contracts.Orders;
using Microsoft.EntityFrameworkCore;
using UTB.Minute.Db.Entities;

namespace UTB.Minute.WebApi.Tests;

/// <summary>
/// Integration tests for Orders API endpoints
/// Tests HTTP API operations for order management
/// </summary>
[Collection("Database collection")]
public class OrderApiTests : IAsyncLifetime
{
    private readonly TestFixture _fixture;

    public OrderApiTests(TestFixture fixture)
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
    /// Test: GET /orders returns all orders
    /// Requirement: Vytvoření a čtení objednávek a jejich testy
    /// </summary>
    [Fact]
    public async Task GetOrders_ReturnsOkWithOrders()
    {
        // Arrange - Create some orders first
        using var context = _fixture.CreateContext();
        var menuItem = await context.MenuItems.FirstAsync(CancellationToken.None);

        var order1 = new Order { MenuItemId = menuItem.Id, StudentName = "Jan Novák", CreatedAtUtc = DateTime.UtcNow, State = OrderState.Preparing };
        var order2 = new Order { MenuItemId = menuItem.Id, StudentName = "Eva Svobodová", CreatedAtUtc = DateTime.UtcNow, State = OrderState.Ready };

        context.Orders.AddRange(order1, order2);
        await context.SaveChangesAsync(CancellationToken.None);

        // Act
        var response = await _fixture.HttpClient.GetAsync("/orders", CancellationToken.None);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var orders = await response.Content.ReadFromJsonAsync<List<OrderDto>>(cancellationToken: CancellationToken.None);

        Assert.NotNull(orders);
        Assert.Equal(2, orders.Count);
    }

    /// <summary>
    /// Test: GET /orders/active returns only non-completed orders
    /// </summary>
    [Fact]
    public async Task GetActiveOrders_ReturnsOkWithActiveOrders()
    {
        // Arrange - Create orders with different states
        using var context = _fixture.CreateContext();
        var menuItem = await context.MenuItems.FirstAsync(CancellationToken.None);

        var order1 = new Order { MenuItemId = menuItem.Id, StudentName = "Jan Novák", CreatedAtUtc = DateTime.UtcNow, State = OrderState.Preparing };
        var order2 = new Order { MenuItemId = menuItem.Id, StudentName = "Eva Svobodová", CreatedAtUtc = DateTime.UtcNow, State = OrderState.Completed };

        context.Orders.AddRange(order1, order2);
        await context.SaveChangesAsync(CancellationToken.None);

        // Act
        var response = await _fixture.HttpClient.GetAsync("/orders/active", CancellationToken.None);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var orders = await response.Content.ReadFromJsonAsync<List<OrderDto>>(cancellationToken: CancellationToken.None);

        Assert.NotNull(orders);
        Assert.Single(orders);
        Assert.Equal(OrderStateDto.Preparing, orders[0].State);
    }

    /// <summary>
    /// Test: GET /orders/{id} returns specific order
    /// </summary>
    [Fact]
    public async Task GetOrderById_WithValidId_ReturnsOkWithOrder()
    {
        // Arrange
        using var context = _fixture.CreateContext();
        var menuItem = await context.MenuItems.FirstAsync(CancellationToken.None);

        var order = new Order { MenuItemId = menuItem.Id, StudentName = "Petr Kovář", CreatedAtUtc = DateTime.UtcNow, State = OrderState.Preparing };
        context.Orders.Add(order);
        await context.SaveChangesAsync(CancellationToken.None);

        // Act
        var response = await _fixture.HttpClient.GetAsync($"/orders/{order.Id}", CancellationToken.None);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var orderDto = await response.Content.ReadFromJsonAsync<OrderDto>(cancellationToken: CancellationToken.None);

        Assert.NotNull(orderDto);
        Assert.Equal(order.Id, orderDto.Id);
        Assert.Equal(order.StudentName, orderDto.StudentName);
        Assert.Equal(OrderStateDto.Preparing, orderDto.State);
    }

    /// <summary>
    /// Test: GET /orders/{id} returns NotFound for invalid ID
    /// </summary>
    [Fact]
    public async Task GetOrderById_WithInvalidId_ReturnsNotFound()
    {
        // Act
        var response = await _fixture.HttpClient.GetAsync("/orders/999", CancellationToken.None);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    /// <summary>
    /// Test: POST /orders creates new order and persists it
    /// Verifies: correct HTTP status (201 Created), DTO in response, Location header, database persistence
    /// Also verifies that available portions are decremented
    /// Requirement: Vytvoření a čtení objednávek a jejich testy
    /// </summary>
    [Fact]
    public async Task CreateOrder_ReturnsCreatedAndPersistsOrder()
    {
        // Arrange
        using var context = _fixture.CreateContext();
        var menuItem = await context.MenuItems.FirstAsync(CancellationToken.None);
        var originalPortions = menuItem.AvailablePortions;

        var createOrderDto = new CreateOrderDto(menuItem.Id, "Tomáš Zelený");

        // Act
        var response = await _fixture.HttpClient.PostAsJsonAsync(
            "/orders",
            createOrderDto,
            CancellationToken.None);

        // Assert - HTTP response
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var orderDto = await response.Content.ReadFromJsonAsync<OrderDto>(cancellationToken: CancellationToken.None);

        Assert.NotNull(orderDto);
        Assert.Equal(createOrderDto.MenuItemId, orderDto.MenuItemId);
        Assert.Equal(createOrderDto.StudentName, orderDto.StudentName);
        Assert.Equal(OrderStateDto.Preparing, orderDto.State);

        // Assert - Location header
        Assert.NotNull(response.Headers.Location);
        Assert.EndsWith($"/orders/{orderDto.Id}", response.Headers.Location.ToString());

        // Assert - Database persistence
        using var contextAfterCreate = _fixture.CreateContext();
        var orderFromDb = await contextAfterCreate.Orders.FindAsync(orderDto.Id, CancellationToken.None);

        Assert.NotNull(orderFromDb);
        Assert.Equal(createOrderDto.StudentName, orderFromDb.StudentName);
        Assert.Equal(OrderState.Preparing, orderFromDb.State);

        // Assert - Available portions decremented
        var menuItemAfter = await contextAfterCreate.MenuItems.FindAsync(menuItem.Id, CancellationToken.None);
        Assert.NotNull(menuItemAfter);
        Assert.Equal(originalPortions - 1, menuItemAfter.AvailablePortions);
    }

    /// <summary>
    /// Test: POST /orders returns BadRequest when menu item does not exist
    /// </summary>
    [Fact]
    public async Task CreateOrder_WithNonExistentMenuItem_ReturnsBadRequest()
    {
        // Arrange
        var createOrderDto = new CreateOrderDto(999, "Test Student");

        // Act
        var response = await _fixture.HttpClient.PostAsJsonAsync(
            "/orders",
            createOrderDto,
            CancellationToken.None);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>
    /// Test: POST /orders returns BadRequest when menu item is sold out
    /// </summary>
    [Fact]
    public async Task CreateOrder_WithSoldOutMenuItem_ReturnsBadRequest()
    {
        // Arrange
        using var context = _fixture.CreateContext();
        var menuItem = await context.MenuItems.FirstAsync(CancellationToken.None);
        menuItem.AvailablePortions = 0;
        await context.SaveChangesAsync(CancellationToken.None);

        var createOrderDto = new CreateOrderDto(menuItem.Id, "Test Student");

        // Act
        var response = await _fixture.HttpClient.PostAsJsonAsync(
            "/orders",
            createOrderDto,
            CancellationToken.None);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>
    /// Test: PUT /orders/{id}/state changes order state
    /// Verifies: correct HTTP status (204 NoContent) and state change in database
    /// Requirement: Změna stavu objednávky a jeho test
    /// </summary>
    [Fact]
    public async Task UpdateOrderState_WithValidTransition_ReturnsNoContentAndUpdatesState()
    {
        // Arrange
        using var context = _fixture.CreateContext();
        var menuItem = await context.MenuItems.FirstAsync(CancellationToken.None);

        var order = new Order { MenuItemId = menuItem.Id, StudentName = "Lukáš Bílý", CreatedAtUtc = DateTime.UtcNow, State = OrderState.Preparing };
        context.Orders.Add(order);
        await context.SaveChangesAsync(CancellationToken.None);

        var updateOrderStateDto = new UpdateOrderStateDto(OrderStateDto.Ready);

        // Act
        var response = await _fixture.HttpClient.PutAsJsonAsync(
            $"/orders/{order.Id}/state",
            updateOrderStateDto,
            CancellationToken.None);

        // Assert - HTTP response
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        // Assert - Database state change
        using var contextAfterUpdate = _fixture.CreateContext();
        var updatedOrder = await contextAfterUpdate.Orders.FindAsync(order.Id, CancellationToken.None);

        Assert.NotNull(updatedOrder);
        Assert.Equal(OrderState.Ready, updatedOrder.State);
    }

    /// <summary>
    /// Test: PUT /orders/{id}/state returns NotFound for invalid ID
    /// </summary>
    [Fact]
    public async Task UpdateOrderState_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        var updateOrderStateDto = new UpdateOrderStateDto(OrderStateDto.Ready);

        // Act
        var response = await _fixture.HttpClient.PutAsJsonAsync(
            "/orders/999/state",
            updateOrderStateDto,
            CancellationToken.None);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    /// <summary>
    /// Test: Order state transitions (Preparing -> Ready -> Completed)
    /// Verifies correct state progression
    /// </summary>
    [Fact]
    public async Task OrderStateTransitions_FollowValidProgression()
    {
        // Arrange
        using var context = _fixture.CreateContext();
        var menuItem = await context.MenuItems.FirstAsync(CancellationToken.None);

        var order = new Order { MenuItemId = menuItem.Id, StudentName = "Aneta Malá", CreatedAtUtc = DateTime.UtcNow, State = OrderState.Preparing };
        context.Orders.Add(order);
        await context.SaveChangesAsync(CancellationToken.None);

        // Act & Assert - Transition 1: Preparing -> Ready
        var updateToReady = new UpdateOrderStateDto(OrderStateDto.Ready);
        var response1 = await _fixture.HttpClient.PutAsJsonAsync(
            $"/orders/{order.Id}/state",
            updateToReady,
            CancellationToken.None);

        Assert.Equal(HttpStatusCode.NoContent, response1.StatusCode);

        using var ctx1 = _fixture.CreateContext();
        var orderAfterReady = await ctx1.Orders.FindAsync(order.Id, CancellationToken.None);
        Assert.Equal(OrderState.Ready, orderAfterReady!.State);

        // Act & Assert - Transition 2: Ready -> Completed
        var updateToCompleted = new UpdateOrderStateDto(OrderStateDto.Completed);
        var response2 = await _fixture.HttpClient.PutAsJsonAsync(
            $"/orders/{order.Id}/state",
            updateToCompleted,
            CancellationToken.None);

        Assert.Equal(HttpStatusCode.NoContent, response2.StatusCode);

        using var ctx2 = _fixture.CreateContext();
        var orderAfterCompleted = await ctx2.Orders.FindAsync(order.Id, CancellationToken.None);
        Assert.Equal(OrderState.Completed, orderAfterCompleted!.State);
    }
}
