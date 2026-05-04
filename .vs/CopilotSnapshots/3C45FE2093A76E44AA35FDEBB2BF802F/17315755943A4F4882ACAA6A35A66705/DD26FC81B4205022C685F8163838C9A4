using FluentAssertions;
using Xunit;
using UTB.Minute.Db;
using UTB.Minute.Db.Entities;
using Microsoft.EntityFrameworkCore;

namespace UTB.Minute.WebApi.Tests;

/// <summary>
/// Integration tests for Meals functionality
/// Tests database operations using EF Core with InMemory provider
/// </summary>
public class MealTests : IAsyncLifetime
{
    private MinuteDbContext _dbContext;

    public async Task InitializeAsync()
    {
        // Create InMemory database context for testing
        var options = new DbContextOptionsBuilder<MinuteDbContext>()
            .UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}")
            .Options;

        _dbContext = new MinuteDbContext(options);
        await _dbContext.Database.EnsureCreatedAsync();
        await SeedTestDataAsync();
    }

    private async Task SeedTestDataAsync()
    {
        var meals = new[]
        {
            new Meal
            {
                Name = "Chicken schnitzel",
                Description = "Crispy chicken schnitzel with potatoes",
                Price = 120,
                IsActive = true
            },
            new Meal
            {
                Name = "Pork goulash",
                Description = "Traditional goulash with pasta",
                Price = 140,
                IsActive = true
            },
            new Meal
            {
                Name = "Fish fillet",
                Description = "Baked fish fillet with spinach",
                Price = 150,
                IsActive = true
            },
            new Meal
            {
                Name = "Vegetarian burger",
                Description = "Homemade burger with herb cheese",
                Price = 110,
                IsActive = true
            }
        };

        _dbContext.Meals.AddRange(meals);
        await _dbContext.SaveChangesAsync();
    }

    public async Task DisposeAsync()
    {
        if (_dbContext != null)
        {
            await _dbContext.Database.EnsureDeletedAsync();
            await _dbContext.DisposeAsync();
        }
    }

    /// <summary>
    /// Test: GET meals should return list of all meals
    /// Requirement: Vytvoření a čtení jídel a jejich testy
    /// </summary>
    [Fact]
    public async Task GetMeals_ShouldReturnListOfMeals()
    {
        // Act
        var meals = await _dbContext.Meals.ToListAsync();

        // Assert
        meals.Should().HaveCount(4);
        meals.All(m => m.IsActive).Should().BeTrue();
        meals.Should().Contain(m => m.Name == "Chicken schnitzel");
        meals.Should().Contain(m => m.Price == 120);
    }

    /// <summary>
    /// Test: POST meal should create new meal
    /// Requirement: Vytvoření a čtení jídel a jejich testy
    /// </summary>
    [Fact]
    public async Task CreateMeal_WithValidData_ShouldCreateMeal()
    {
        // Arrange
        var newMeal = new Meal
        {
            Name = "Test Meal",
            Description = "Test Description",
            Price = 99.99m,
            IsActive = true
        };

        // Act
        _dbContext.Meals.Add(newMeal);
        await _dbContext.SaveChangesAsync();

        // Assert
        var mealFromDb = await _dbContext.Meals.FirstOrDefaultAsync(m => m.Name == "Test Meal");
        mealFromDb.Should().NotBeNull();
        mealFromDb.Price.Should().Be(99.99m);
        mealFromDb.IsActive.Should().BeTrue();
    }

    /// <summary>
    /// Test: GET meal by ID should return specific meal
    /// </summary>
    [Fact]
    public async Task GetMealById_WithValidId_ShouldReturnMeal()
    {
        // Arrange
        var meals = await _dbContext.Meals.ToListAsync();
        var firstMeal = meals.First();

        // Act
        var mealFromDb = await _dbContext.Meals.FindAsync(firstMeal.Id);

        // Assert
        mealFromDb.Should().NotBeNull();
        mealFromDb.Id.Should().Be(firstMeal.Id);
        mealFromDb.Name.Should().Be(firstMeal.Name);
        mealFromDb.Description.Should().Be(firstMeal.Description);
    }

    /// <summary>
    /// Test: PUT meal should update meal and allow deactivation
    /// Requirement: Úprava jídla + deaktivace a jejich testy
    /// </summary>
    [Fact]
    public async Task UpdateMeal_WithValidData_ShouldUpdateMeal()
    {
        // Arrange
        var meals = await _dbContext.Meals.ToListAsync();
        var mealToUpdate = meals.First();
        var originalId = mealToUpdate.Id;

        // Act
        mealToUpdate.Name = "Updated Name";
        mealToUpdate.Description = "Updated Description";
        mealToUpdate.Price = 199.99m;
        mealToUpdate.IsActive = false;

        _dbContext.Meals.Update(mealToUpdate);
        await _dbContext.SaveChangesAsync();

        // Assert
        var updatedMeal = await _dbContext.Meals.FindAsync(originalId);
        updatedMeal.Should().NotBeNull();
        updatedMeal.Name.Should().Be("Updated Name");
        updatedMeal.Description.Should().Be("Updated Description");
        updatedMeal.Price.Should().Be(199.99m);
        updatedMeal.IsActive.Should().BeFalse();
    }

    /// <summary>
    /// Test: GET meal with invalid ID should return null (not found)
    /// </summary>
    [Fact]
    public async Task GetMealById_WithInvalidId_ShouldReturnNull()
    {
        // Act
        var mealFromDb = await _dbContext.Meals.FindAsync(999);

        // Assert
        mealFromDb.Should().BeNull();
    }

    /// <summary>
    /// Test: Deactivating meal (instead of deleting) preserves data
    /// Requirement: Jídlo se neodstraňuje, pouze se označí jako neaktivní
    /// </summary>
    [Fact]
    public async Task DeactivateMeal_ShouldMarkAsInactive()
    {
        // Arrange
        var meals = await _dbContext.Meals.ToListAsync();
        var mealToDeactivate = meals.First();

        // Act
        mealToDeactivate.IsActive = false;
        _dbContext.Meals.Update(mealToDeactivate);
        await _dbContext.SaveChangesAsync();

        // Assert
        var deactivatedMeal = await _dbContext.Meals.FindAsync(mealToDeactivate.Id);
        deactivatedMeal.Should().NotBeNull(); // Still in database
        deactivatedMeal.IsActive.Should().BeFalse();

        // Verify data integrity
        deactivatedMeal.Name.Should().Be(mealToDeactivate.Name);
        deactivatedMeal.Price.Should().Be(mealToDeactivate.Price);
    }

    /// <summary>
    /// Test: Can filter active meals only
    /// </summary>
    [Fact]
    public async Task GetActiveMeals_ShouldExcludeInactiveMeals()
    {
        // Arrange
        var meals = await _dbContext.Meals.ToListAsync();
        var mealToDeactivate = meals.First();
        mealToDeactivate.IsActive = false;
        _dbContext.Meals.Update(mealToDeactivate);
        await _dbContext.SaveChangesAsync();

        // Act
        var activeMeals = await _dbContext.Meals.Where(m => m.IsActive).ToListAsync();

        // Assert
        activeMeals.Should().HaveCount(3);
        activeMeals.Should().NotContain(m => m.Id == mealToDeactivate.Id);
    }
}
