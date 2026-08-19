# create-pptx Command

## Overview
The `create-pptx` command has been added to the UITestForge script editor to generate PowerPoint reports automatically during script execution.

## Syntax
```
create-pptx [filename] [title]
```

### Arguments
- **filename** (optional): The name of the PowerPoint file to create
  - If omitted, auto-generates: `report_yyyyMMdd_HHmmss.pptx`
  - If no extension provided, `.pptx` is automatically appended
  - Relative paths are saved to the app data directory
  - Absolute paths are supported

- **title** (optional): The title to display on the report slide
  - If omitted, defaults to: `"Test Report"`

## How It Works
The `create-pptx` command automatically collects:
- **Before Screenshot**: The first screenshot taken in the script
- **After Screenshot**: The last screenshot taken in the script
- **Execution Logs**: All command output up to the point when create-pptx is called
- **Script Text**: The complete script being executed

These elements are combined into a single PowerPoint slide with three columns using `PptxReportHelper.CreateReport()`.

## Example Usage

### Basic Usage (auto-generated filename)
```devflow
screenshot
tap CounterBtn
wait 1
screenshot
create-pptx
```
Result: Creates `report_20250605_143022.pptx` (timestamp-based name)

### With Custom Filename
```devflow
screenshot before.png
tap CounterBtn
screenshot after.png
create-pptx my_test_report.pptx
```
Result: Creates `my_test_report.pptx`

### With Custom Filename and Title
```devflow
screenshot
tap CounterBtn
wait 2
tap CounterBtn
screenshot
create-pptx counter_test.pptx "Counter Button Test"
```
Result: Creates `counter_test.pptx` with title "Counter Button Test"

## Implementation Details

### Files Modified
- **UITestForge/Helpers/ScriptEditorHelper.cs**
  - Added screenshot tracking (`firstScreenshot`, `lastScreenshot`)
  - Added `create-pptx` to `BuildCliArgs` switch statement
  - Added `HandleCreatePptxAsync` method to process the command
  - Updated `RunScriptAsync` to track screenshots and handle create-pptx command

### Integration with PptxReportHelper
The command calls:
```csharp
PptxReportHelper.CreateReport(
   outputPath,
   beforeImagePath,
   afterImagePath,
   executionLogs,
   scriptText,
   title);
```

### Platform Compatibility
- ✅ Windows (supported)
- ❌ Android (not supported - script execution requires Windows)
- ✅ iOS/macOS (supported if CLI tools are available)

The entire `ScriptEditorHelper` class is wrapped in `#if !ANDROID` to match the availability of `DevFlowCliHelper.RunDevFlowAsync`.

## Output Location
- **Relative paths**: `FileSystem.AppDataDirectory` (app data folder)
- **Absolute paths**: Specified location

The command outputs the full path of the created file to the execution log:
```
✓ PowerPoint created → C:\Users\...\AppData\Local\...\report_20250605_143022.pptx
```

## Error Handling
If the PowerPoint creation fails, the command outputs an error message:
```
✗ Failed to create PowerPoint: [error details]
```

## Sample Script
See `UITestForge/SampleScripts/demo_with_report.devflow` for a complete example.
