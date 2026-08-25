# Auto-Refresh Feature for Page Check Commands

## Overview

The `checkpage` and `checknpage` commands now automatically refresh the UITestForge visual tree before checking the current page. This ensures that the page check always uses the most up-to-date information about which page is currently displayed.

## Implementation Details

### Architecture

The auto-refresh feature is implemented through a callback mechanism:

1. **Callback Parameter**: `RunScriptAsync` now accepts an optional `onGetCurrentPage` callback parameter
   ```csharp
   Func<Task<string?>>? onGetCurrentPage = null
   ```

2. **Refresh Method**: New method in `MainViewModel` that refreshes the tree and returns current page:
   ```csharp
   internal async Task<string?> RefreshAndGetPageNameAsync()
   ```

3. **Execution Flow**:
   - When `checkpage` or `checknpage` is encountered
   - If `onGetCurrentPage` callback is provided, it's called to refresh the tree
   - The fresh page name is used for comparison
   - If callback is null, falls back to the `currentPageName` parameter

### Files Modified

#### 1. ScriptEditorHelper.cs
- Added `onGetCurrentPage` parameter to `RunScriptAsync`
- Updated `checkpage` command to call refresh callback before checking
- Updated `checknpage` command to call refresh callback before checking
- Updated recursive `call` command to pass callback through

#### 2. MainViewModel.cs
- Added `RefreshAndGetPageNameAsync()` method
- Performs tree refresh using DevFlow CLI
- Extracts and returns current page name from tree structure

#### 3. MainPage.xaml.cs
- Updated `OnScriptRunClicked` to pass callback:
  ```csharp
  onGetCurrentPage: async () => await _viewModel.RefreshAndGetPageNameAsync()
  ```

#### 4. Documentation
- Updated `page-check-commands.md` to document auto-refresh behavior
- Added tips about performance considerations

#### 5. Sample Scripts
- Created `auto_refresh_page_check.devflow` demonstrating the feature

## Benefits

### 1. **Accuracy**
   - Always uses current page state
   - No stale page information
   - Reliable conditional logic

### 2. **Simplicity**
   - No manual refresh commands needed
   - One less thing to remember
   - Cleaner scripts

### 3. **Robustness**
   - Handles page transitions automatically
   - Works even when page changes between commands
   - Reduces race conditions

## Usage Examples

### Before (Manual Refresh Required)
```devflow
# Old way - would need manual refresh or risk stale data
tap NavigateToSettingsBtn
wait 2
# Hope the page loaded...
checkpage SettingsPage onSettings
```

### After (Automatic Refresh)
```devflow
# New way - automatic refresh ensures current state
tap NavigateToSettingsBtn
wait 2
checkpage SettingsPage onSettings  # Automatically refreshes tree first
```

## Performance Considerations

Each `checkpage` or `checknpage` command triggers a tree refresh, which involves:
- CLI call to DevFlow agent
- JSON parsing
- UI tree reconstruction

**Best Practices:**
- ✅ Use for page navigation logic
- ✅ Use after actions that change pages
- ✅ Use at decision points in scripts
- ❌ Avoid in tight loops or repeated checks
- ❌ Don't use when page definitely hasn't changed

## Error Handling

If the refresh fails:
1. Error is logged in the script output
2. Execution continues to next line
3. No jump is performed (safe fallback)

Example error message:
```
✗ failed to refresh page name: Could not connect to agent
```

## Backward Compatibility

The feature is fully backward compatible:
- The `onGetCurrentPage` parameter is optional
- If null, the commands use the `currentPageName` parameter as before
- Existing scripts continue to work without modification

## Technical Note

The refresh is performed using the same mechanism as the manual "Refresh Tree" button:
```csharp
var (exitCode, stdout, stderr) = await DevFlowCliHelper.RunDevFlowAsync(
    "ui tree",
    SelectedAgent);
```

This ensures consistency between manual and automatic refresh operations.
