# Testing Guide - Blazor Filtering Implementation

## Overview
This guide explains how to test the new filtering functionality added to the Meals and Menu Items management pages in the AdminClient.

## Quick Start Testing Steps

### 1. Start the Application
```powershell
cd C:\Users\xs8x\Documents\AP_project_v3\UTB
dotnet run --project .\UTB.Minute.AppHost
```

Wait for the Aspire dashboard to appear and click on the `adminclient` or `web` project link.

### 2. Navigate to Meals Page
- URL: `https://localhost:port/meals`
- You should see a list of meals in a table

### 3. Test Filter Buttons

**Test "All" Button:**
1. Click the "All" button in the filter group
2. Verify all meals are displayed in the table
3. No filtering should be applied

**Test "Active" Button:**
1. Click the "Active" button
2. Verify only meals with IsActive=true are displayed
3. The "Active" button should be highlighted in primary blue

**Test "Inactive" Button:**
1. Click the "Inactive" button
2. Verify only meals with IsActive=false are displayed
3. The "Inactive" button should be highlighted in primary blue

### 4. Verify Button State Indication
- The currently selected filter button appears in **btn-primary** (solid blue)
- Non-selected buttons appear in **btn-outline-primary** (outline only)
- This provides clear visual feedback of the active filter

### 5. Test Filter Persistence After CRUD Operations

**After Creating a Meal:**
1. With "Active" filter selected, click "New Meal"
2. Create a new active meal
3. After creation, the list should refresh and still show only active meals

**After Editing a Meal:**
1. Select "Active" filter
2. Click Edit on an active meal
3. Change IsActive to false and save
4. The page should refresh and the meal disappears (filter still active)

**After Deleting a Meal:**
1. Select any filter (e.g., "Inactive")
2. Meals matching that filter are deleted
3. The list refreshes with the current filter still applied

### 6. Test Menu Items Page

**Navigate to Menu Items:**
- URL: `https://localhost:port/menu-items`

**Test "Not Archived" Button:**
1. Click "Not Archived" button
2. Should display only menu items with Date >= Today
3. Future and today's menu items should be visible

**Test "Archived" Button:**
1. Click "Archived" button
2. Should display only menu items with Date < Today
3. Past menu items should be visible

**Test "All" Button:**
1. Click "All" button
2. All menu items (past and future) should display

## Expected Behavior

### Data Filtering
- Filters are applied immediately when button is clicked
- Loading indicator shows while data is being fetched
- No page reload necessary (Interactive Server mode)

### Button Highlighting
```
Before click:
[All] [Active] [Inactive]
 (outlined)

After clicking Active:
[All] [Active] [Inactive]
 (outlined) (filled) (outlined)
```

### Network Requests
The following HTTP requests should be visible in browser DevTools:
- `GET /meals` - When "All" is clicked
- `GET /meals?isActive=true` - When "Active" is clicked
- `GET /meals?isActive=false` - When "Inactive" is clicked

Similarly for menu items with `?isArchived=true` and `?isArchived=false`

## Troubleshooting

### No Data Shows After Filter Click
**Symptom:** Filter buttons work but no meals/items appear

**Solution:**
1. Open browser Developer Tools (F12)
2. Check the Console tab for JavaScript errors
3. Check the Network tab to verify API responses
4. Ensure WebApi is running and responding correctly

### Button Click Not Responding
**Symptom:** Clicking filter buttons has no effect

**Solution:**
1. Verify you're on a page with `@rendermode @(new InteractiveServerRenderMode())`
2. Check browser console for JavaScript errors
3. Ensure the page is fully loaded (no "Loading..." message)
4. Try hard refresh: Ctrl+Shift+R

### 404 Error on Filter
**Symptom:** Browser DevTools shows 404 error for `/meals?isActive=true`

**Solution:**
1. Verify WebApi endpoint changes were compiled
2. Restart the WebApi application
3. Check that filter parameter syntax is correct
4. Review WebApi Program.cs GetMeals method signature

### Filter State Not Preserved
**Symptom:** After creating/editing, filter resets to "All"

**Solution:**
1. This is expected behavior - filter is preserved
2. If it's not, check LoadMeals() method is being called
3. Verify selectedFilter state variable is set correctly

## Performance Notes

- Filtering is done server-side for better performance
- Only requested data is transferred over network
- UI updates immediately without page reload

## Code Review Points

### API Client Method Overloading
The ApiClient uses method overloading for clean code:
```csharp
// Gets all meals
await Api.GetMealsAsync()

// Gets only active meals
await Api.GetMealsAsync(true)
```

### State Management
Filter state is stored as `bool?` in the component:
```csharp
private bool? selectedFilter = null;
```
- `null` = All (no filter)
- `true` = Active/Archived
- `false` = Inactive/Not Archived

### Filter Method Pattern
```csharp
private async Task FilterMeals(bool? filter)
{
    selectedFilter = filter;  // Update state
    await LoadMeals();         // Reload data with new filter
}
```

## Success Criteria

✅ All filter buttons are clickable  
✅ Active button is visually highlighted when selected  
✅ Data filters correctly based on selection  
✅ Filter persists during CRUD operations  
✅ No page reload occurs when filtering  
✅ Loading indicator appears during data fetch  
✅ API is called with correct query parameters  
✅ No console errors in browser Developer Tools  

## Additional Test Scenarios

1. **Network Lag Simulation:**
   - Open DevTools > Network tab
   - Set throttling to "Slow 3G"
   - Click filter buttons and verify behavior

2. **Rapid Filter Clicks:**
   - Quickly click different filter buttons
   - Verify data eventually shows correctly
   - No race conditions occur

3. **Empty Result Sets:**
   - If no inactive meals exist, click "Inactive"
   - Should show "No meals found" message
   - Filter button should still work

4. **Multiple Clients:**
   - Open the page in two browser tabs
   - Apply different filters in each
   - Create new meal in one tab
   - Verify the other tab's filter still works when refreshed

## Summary

The filtering implementation provides:
- **User-Friendly:** Clear button interface with visual feedback
- **Responsive:** Immediate data updates without page reload
- **Efficient:** Server-side filtering reduces network traffic
- **Maintainable:** Clean code structure with method overloading
- **Reliable:** Proper error handling and state management
