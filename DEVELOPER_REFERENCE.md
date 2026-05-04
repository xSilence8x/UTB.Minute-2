# Developer Quick Reference - Filtering Implementation

## Files Modified

### 1. `UTB.Minute.AdminClient\Services\ApiClient.cs`
**Changes:** Added overloaded filter methods

```csharp
// New methods added (lines 22-31 and 53-62)
public async Task<List<MealDto>> GetMealsAsync(bool? isActive)
public async Task<List<MenuItemDto>> GetMenuItemsAsync(bool? isArchived)
```

### 2. `UTB.Minute.AdminClient\Components\Pages\Meals.razor`
**Changes:** Added filter UI and state management

- **Lines 22-30:** Filter button group UI
- **Line 139:** Filter state variable `private bool? selectedFilter = null;`
- **Lines 145-158:** Updated LoadMeals() method
- **Lines 160-165:** New FilterMeals() method

### 3. `UTB.Minute.AdminClient\Components\Pages\MenuItems.razor`
**Changes:** Added filter UI and state management

- **Lines 23-31:** Filter button group UI
- **Line 138:** Filter state variable `private bool? selectedFilter = null;`
- **Lines 145-163:** Updated LoadData() method
- **Lines 165-170:** New FilterMenuItems() method

### 4. `UTB.Minute.WebApi\Program.cs`
**Changes:** Updated endpoint handlers to support query parameters

- **Lines 63-81:** Modified GetMeals() to accept `bool? isActive`
- **Lines 141-165:** Modified GetMenuItems() to accept `bool? isArchived`

## Architecture Diagram

```
User Interface (Razor Components)
    ↓
    [Filter Button Click]
    ↓
FilterMeals(bool?) / FilterMenuItems(bool?)
    ↓
[Update selectedFilter state]
    ↓
LoadMeals() / LoadData()
    ↓
ApiClient.GetMealsAsync(filter?) / GetMenuItemsAsync(filter?)
    ↓
[Build query string with optional parameters]
    ↓
HTTP GET Request
/meals?isActive=true  or
/menu-items?isArchived=false
    ↓
WebApi Endpoint Handler
    ↓
[Apply LINQ Where clause if parameter present]
    ↓
Database Query
    ↓
[Return filtered results]
    ↓
JSON Response
    ↓
Deserialize to List<DTO>
    ↓
[Update meals / menuItems variable]
    ↓
Component Re-renders
    ↓
Filtered Data Displayed
```

## Key Code Snippets

### Filter Button HTML
```razor
<div class="btn-group" role="group">
    <button class="btn @(selectedFilter == null ? "btn-primary" : "btn-outline-primary")" 
            @onclick="() => FilterMeals(null)">All</button>
    <button class="btn @(selectedFilter == true ? "btn-primary" : "btn-outline-primary")" 
            @onclick="() => FilterMeals(true)">Active</button>
    <button class="btn @(selectedFilter == false ? "btn-primary" : "btn-outline-primary")" 
            @onclick="() => FilterMeals(false)">Inactive</button>
</div>
```

### Service Filter Method
```csharp
public async Task<List<MealDto>> GetMealsAsync(bool? isActive)
{
    if (isActive == null)
    {
        return await GetMealsAsync();  // Get all
    }

    var query = isActive.Value ? "?isActive=true" : "?isActive=false";
    return await _httpClient.GetFromJsonAsync<List<MealDto>>($"/meals{query}") ?? [];
}
```

### Component Filter Method
```csharp
private async Task FilterMeals(bool? filter)
{
    selectedFilter = filter;
    await LoadMeals();
}

private async Task LoadMeals()
{
    try
    {
        isLoading = true;
        if (selectedFilter == null)
        {
            meals = await Api.GetMealsAsync();
        }
        else
        {
            meals = await Api.GetMealsAsync(selectedFilter);
        }
    }
    finally
    {
        isLoading = false;
    }
}
```

### WebApi Endpoint
```csharp
static async Task<Ok<List<MealDto>>> GetMeals(bool? isActive, MinuteDbContext db)
{
    var query = db.Meals.AsQueryable();

    if (isActive.HasValue)
    {
        query = query.Where(m => m.IsActive == isActive.Value);
    }

    var meals = await query
        .Select(m => new MealDto(m.Id, m.Name, m.Description, m.Price, m.IsActive))
        .ToListAsync();

    return TypedResults.Ok(meals);
}
```

## Common Patterns

### Pattern 1: Nullable Bool for Filter State
```csharp
private bool? selectedFilter = null;

// null = no filter (All)
// true = show active/archived
// false = show inactive/not archived
```

### Pattern 2: Method Overloading
```csharp
// Two versions of the same method
public async Task<List<T>> GetAsync()           // No filter
public async Task<List<T>> GetAsync(bool? flag) // With filter
```

### Pattern 3: Query String Building
```csharp
var query = isActive.Value ? "?isActive=true" : "?isActive=false";
// Produces: GET /meals?isActive=true
```

### Pattern 4: Conditional LINQ Filtering
```csharp
var query = db.Meals.AsQueryable();

if (isActive.HasValue)
{
    query = query.Where(m => m.IsActive == isActive.Value);
}

var result = await query.ToListAsync();
```

## Testing Checklist

- [ ] Meals filter "All" works
- [ ] Meals filter "Active" works
- [ ] Meals filter "Inactive" works
- [ ] Menu Items filter "All" works
- [ ] Menu Items filter "Not Archived" works
- [ ] Menu Items filter "Archived" works
- [ ] Filter buttons highlight correctly
- [ ] Create operation works with active filter
- [ ] Edit operation works with active filter
- [ ] Delete operation works with active filter
- [ ] Network requests show correct query params
- [ ] No console errors in DevTools
- [ ] Loading indicator appears during fetch
- [ ] Filter persists after CRUD operations

## Extending to Other Entities

To add filtering to a new entity (e.g., Books):

1. **Service Layer:**
   ```csharp
   public async Task<List<BookDto>> GetBooksAsync()
   public async Task<List<BookDto>> GetBooksAsync(bool? isArchived)
   ```

2. **WebApi Endpoint:**
   ```csharp
   static async Task<Ok<List<BookDto>>> GetBooks(bool? isArchived, DbContext db)
   {
       var query = db.Books.AsQueryable();
       if (isArchived.HasValue)
           query = query.Where(b => b.IsArchived == isArchived.Value);
       // ... rest of implementation
   }
   ```

3. **Razor Component:**
   ```razor
   @page "/books"

   <div class="btn-group">
       <button @onclick="() => FilterBooks(null)">All</button>
       <button @onclick="() => FilterBooks(true)">Archived</button>
       <button @onclick="() => FilterBooks(false)">Not Archived</button>
   </div>
   ```

4. **Component Code:**
   ```csharp
   private bool? selectedFilter = null;

   private async Task FilterBooks(bool? filter)
   {
       selectedFilter = filter;
       await LoadBooks();
   }
   ```

## Performance Considerations

✅ **What works well:**
- Server-side filtering reduces network bandwidth
- Query executed at database level
- Index usage on IsActive property recommended
- Loading state prevents duplicate requests

⚠️ **Things to watch:**
- Large result sets may need pagination
- Multiple concurrent filter requests possible
- Database index on filter columns recommended

## Debugging Tips

### DevTools Network Tab
1. Open F12 Developer Tools
2. Click Network tab
3. Apply a filter
4. Look for `/meals?isActive=true` requests
5. Check response status (200 OK expected)
6. Inspect JSON payload

### Browser Console
```javascript
// Should see no errors
// Check if SignalR connection is active (for Interactive Server mode)
// Verify component interactivity indicator appears
```

### Visual Studio Debugger
1. Set breakpoint in `FilterMeals()` method
2. Step through to see state updates
3. Check `meals` variable contents after filter
4. Verify `selectedFilter` value changes

## Related Documentation

- **IMPLEMENTATION_SUMMARY.md** - Complete implementation details
- **TESTING_GUIDE.md** - How to test the filtering
- **ASSIGNMENT_FULFILLMENT.md** - How this meets assignment requirements

## Quick Commands

### Build and Run
```powershell
cd C:\Users\xs8x\Documents\AP_project_v3\UTB
dotnet run --project .\UTB.Minute.AppHost
```

### Run Tests (if available)
```powershell
dotnet test
```

### Clean Build
```powershell
dotnet clean
dotnet build
```

## Troubleshooting Quick Reference

| Issue | Cause | Solution |
|-------|-------|----------|
| Filter buttons not responding | Not interactive mode | Verify `@rendermode InteractiveServer` |
| 404 on filter request | API not updated | Rebuild WebApi project |
| Data not filtering | Service not called correctly | Check LoadMeals() implementation |
| Button not highlighting | Wrong comparison logic | Check selectedFilter state variable |
| Stale data after filter | Cache not cleared | Verify LoadMeals() reloads all data |
| Console errors | JavaScript issue | Check browser DevTools Console |

## Performance Metrics

Expected performance:
- Filter button click to data display: ~100-500ms
- Network latency: 50-200ms
- Database query: 10-50ms
- UI re-render: 10-30ms

Typical sequence:
```
Button Click (instant)
  → 50ms (network round-trip)
  → 20ms (database query)
  → 30ms (deserialization)
  → 20ms (UI render)
  ≈ ~120ms total user-perceived delay
```

## Browser Compatibility

✅ Supported:
- Chrome 90+
- Firefox 88+
- Edge 90+
- Safari 14+

Requires WebAssembly support and WebSocket (for SignalR)
