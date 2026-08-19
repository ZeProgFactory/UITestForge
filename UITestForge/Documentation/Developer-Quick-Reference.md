# UITestForge - Developer Quick Reference

## MVVM Architecture Overview

```
┌─────────────────────────────────────────────────────────┐
│                     MainPage.xaml                        │
│                  (Declarative Bindings)                  │
└────────────────────┬────────────────────────────────────┘
                     │ Binds to
                     ▼
┌─────────────────────────────────────────────────────────┐
│                   MainViewModel.cs                       │
│              (Business Logic & State)                    │
│  • ObservableProperties (19)                            │
│  • RelayCommands (5)                                     │
│  • Observable Collections (2)                            │
└────────────────────┬────────────────────────────────────┘
                     │ Uses
                     ▼
┌─────────────────────────────────────────────────────────┐
│                   Helpers & Services                     │
│  • DevFlowCliHelper                                      │
│  • ScriptEditorHelper                                    │
│  • PptxReportHelper                                      │
│  • VisualTreeHelper                                      │
└─────────────────────────────────────────────────────────┘
```

## Adding a New Feature (Step-by-Step)

### 1. Add Observable Property to ViewModel

```csharp
// UITestForge/ViewModels/MainViewModel.cs

[ObservableProperty]
private string _myNewProperty = "default value";
```

**Generated automatically**:
- Property: `MyNewProperty`
- Change notification: `OnMyNewPropertyChanged()` partial method
- Property changed event: `PropertyChanged?.Invoke(...)`

### 2. Bind Property in XAML

```xml
<!-- UITestForge/MainPage.xaml -->

<Label Text="{Binding MyNewProperty}" />
<Entry Text="{Binding MyNewProperty}" />
<Switch IsToggled="{Binding MyNewProperty}" />
```

### 3. Add Command to ViewModel

```csharp
// UITestForge/ViewModels/MainViewModel.cs

[RelayCommand]
private async Task DoSomethingAsync()
{
    IsBusy = true;
    try
    {
        // Your logic here
        MyNewProperty = "updated value";
    }
    finally
    {
        IsBusy = false;
    }
}
```

**Generated automatically**:
- Command: `DoSomethingCommand`
- Type: `IAsyncRelayCommand`
- Can execute: Automatic handling

### 4. Bind Command in XAML

```xml
<!-- UITestForge/MainPage.xaml -->

<Button Text="Do Something" 
        Command="{Binding DoSomethingCommand}" />
```

### 5. Add Event Handler (if needed)

```csharp
// UITestForge/MainPage.xaml.cs

private async void OnSomethingClicked(object? sender, EventArgs e)
{
    // UI-specific logic only (file pickers, dialogs, etc.)
    var result = await DisplayAlert("Confirm", "Are you sure?", "Yes", "No");
    if (result)
    {
        _viewModel.DoSomethingCommand.Execute(null);
    }
}
```

## Common Patterns

### Pattern 1: Loading Data

```csharp
// ViewModel
[RelayCommand]
private async Task LoadDataAsync()
{
    IsBusy = true;
    StatusText = "Loading...";

    try
    {
        var data = await SomeService.GetDataAsync();
        MyCollection.Clear();
        foreach (var item in data)
            MyCollection.Add(item);

        StatusText = $"Loaded {data.Count} items";
    }
    catch (Exception ex)
    {
        StatusText = $"Error: {ex.Message}";
    }
    finally
    {
        IsBusy = false;
    }
}
```

### Pattern 2: Property Change Reactions

```csharp
// ViewModel - partial method auto-generated
partial void OnSelectedAgentChanged(DevFlowAgent? value)
{
    // React to selection change
    UpdateAgentDetails(value);
    TapCounterEnabled = value != null;
}
```

### Pattern 3: Conditional Command Execution

```csharp
// ViewModel
[RelayCommand(CanExecute = nameof(CanExecuteAction))]
private void ExecuteAction()
{
    // Action logic
}

private bool CanExecuteAction()
{
    return SelectedAgent != null && !IsBusy;
}

// Call when conditions change
private void UpdateConditions()
{
    ExecuteActionCommand.NotifyCanExecuteChanged();
}
```

### Pattern 4: UI-Specific Operations in View

```csharp
// MainPage.xaml.cs
private async void OnShareClicked(object? sender, EventArgs e)
{
    // Platform-specific sharing
    await Share.RequestAsync(new ShareFileRequest
    {
        Title = "Share Screenshot",
        File = new ShareFile(imagePath)
    });
}
```

## Script Editor Commands

### Available Commands

| Command | Syntax | Description |
|---------|--------|-------------|
| `tap` | `tap <automationId>` | Tap an element |
| `fill` | `fill <automationId> <text>` | Fill text input |
| `clear` | `clear <automationId>` | Clear text input |
| `focus` | `focus <automationId>` | Focus an element |
| `navigate` | `navigate <route>` | Navigate to route |
| `scroll` | `scroll <direction\|id>` | Scroll page or element |
| `screenshot` | `screenshot [path]` | Take screenshot |
| `wait` | `wait <seconds>` | Wait/delay |
| `create-pptx` | `create-pptx [file] [title]` | Generate report |

### Example Script

```devflow
# Demo script
screenshot before.png
tap CounterBtn
wait 2
tap CounterBtn
screenshot after.png
create-pptx demo.pptx "Counter Test"
```

### Adding a New Command

1. **Add to BuildCliArgs** (`ScriptEditorHelper.cs`):
```csharp
"mycommand" => ParseMyCommand(rest),
```

2. **Add parser method**:
```csharp
private static string ParseMyCommand(string rest)
{
    if (string.IsNullOrWhiteSpace(rest))
        throw new ArgumentException("mycommand requires arguments");
    return $"ui mycommand --arg \"{rest.Trim()}\"";
}
```

3. **Add to RunScriptAsync** (if special handling needed):
```csharp
else if (cmd == "mycommand")
{
    resultLine = await HandleMyCommandAsync(rest, agent);
}
```

## File Locations

### Core Files
- **ViewModel**: `UITestForge/ViewModels/MainViewModel.cs`
- **View Code**: `UITestForge/MainPage.xaml.cs`
- **View XAML**: `UITestForge/MainPage.xaml`

### Helpers
- **Script Editor**: `UITestForge/Helpers/ScriptEditorHelper.cs`
- **PPTX Reports**: `UITestForge/Helpers/PptxReportHelper.cs`
- **DevFlow CLI**: `UITestForge/Helpers/DevFlowCliHelper.cs`
- **Visual Tree**: `UITestForge/Helpers/VisualTreeHelper.cs`

### Documentation
- **MVVM Summary**: `UITestForge/Documentation/MVVM-Migration-Summary.md`
- **PPTX Command**: `UITestForge/Documentation/create-pptx-command.md`
- **Verification**: `UITestForge/Documentation/MVVM-Verification-Checklist.md`
- **Quick Reference**: `UITestForge/Documentation/Developer-Quick-Reference.md` (this file)

### Sample Scripts
- **PPTX Demo**: `UITestForge/SampleScripts/demo_with_report.devflow`

## Debugging Tips

### 1. Binding Issues
- Check Output window for binding errors
- Verify property names match exactly (case-sensitive)
- Ensure BindingContext is set
- Use `x:Name` for code-behind access

### 2. Command Not Executing
- Check `CanExecute` logic
- Verify command is async if method is async
- Call `NotifyCanExecuteChanged()` when conditions change
- Check for exceptions in command handler

### 3. Property Not Updating
- Verify `[ObservableProperty]` attribute
- Check for typos in XAML binding
- Ensure property is public or internal
- Look for PropertyChanged events in Output

### 4. Collection Changes Not Reflected
- Use `ObservableCollection<T>`, not `List<T>`
- Clear and re-add instead of replacing collection
- For Picker: force ItemsSource refresh (see `OnAgentsCollectionChanged`)

## Testing Recommendations

### Unit Testing ViewModel

```csharp
[Test]
public async Task ToggleMonitoring_StartsMonitoring()
{
    // Arrange
    var vm = new MainViewModel();

    // Act
    await vm.ToggleMonitoringCommand.ExecuteAsync(null);

    // Assert
    Assert.IsTrue(vm.IsMonitoring);
    Assert.AreEqual("Stop", vm.MonitorButtonText);
}
```

### Integration Testing

```csharp
[Test]
public async Task TakeScreenshot_CapturesImage()
{
    // Arrange
    var vm = new MainViewModel();
    vm.SelectedAgent = CreateTestAgent();
    string? capturedPath = null;
    vm.ScreenshotCaptured += (s, path) => capturedPath = path;

    // Act
    await vm.TakeScreenshotCommand.ExecuteAsync(null);

    // Assert
    Assert.IsNotNull(capturedPath);
    Assert.IsTrue(File.Exists(capturedPath));
}
```

## Performance Tips

1. **Avoid expensive operations in property setters**
2. **Use async commands for I/O operations**
3. **Debounce rapid property changes**
4. **Dispose resources in ViewModel destructor**
5. **Use weak event handlers where appropriate**

## Best Practices

### ✅ DO
- Keep business logic in ViewModel
- Use `[ObservableProperty]` for state
- Use `[RelayCommand]` for actions
- Bind UI elements to ViewModel properties
- Handle UI-specific concerns in code-behind
- Use async/await for I/O operations
- Set `IsBusy` during long operations

### ❌ DON'T
- Put business logic in code-behind
- Directly manipulate UI elements from ViewModel
- Use string-based property change notifications
- Create commands manually
- Block UI thread with synchronous operations
- Forget to handle exceptions in commands
- Ignore `CanExecute` logic

## Quick Commands Reference

### Start/Stop Monitoring
```xml
<Button Text="{Binding MonitorButtonText}" 
        Command="{Binding ToggleMonitoringCommand}" />
```

### Take Screenshot
```xml
<Button Text="📸 Screenshot"
        Command="{Binding TakeScreenshotCommand}"
        IsEnabled="{Binding CanTakeScreenshot}" />
```

### Refresh Tree
```xml
<Button Text="🔄"
        Command="{Binding RefreshTreeCommand}" />
```

### Show Busy Indicator
```xml
<ActivityIndicator IsRunning="{Binding IsBusy}"
                   IsVisible="{Binding IsBusy}" />
```

## Need Help?

1. Check the **MVVM Migration Summary** for architecture details
2. Review **create-pptx-command.md** for script command examples
3. Read **MVVM-Verification-Checklist.md** for completed features
4. Examine existing commands in `ScriptEditorHelper.cs`
5. Look at `MainViewModel.cs` for property/command patterns

---

**Last Updated**: Migration completed successfully
**Framework**: .NET 10 MAUI
**MVVM Toolkit**: CommunityToolkit.Mvvm
