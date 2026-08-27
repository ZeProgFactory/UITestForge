# UITestForge

**Put your MAUI app on autopilot.**

UITestForge is a cross-platform [.NET MAUI](https://learn.microsoft.com/dotnet/maui/) companion app that lets you drive, inspect, and test another running MAUI app in real time. Using [Microsoft.Maui.DevFlow.Agent](https://www.nuget.org/packages/Microsoft.Maui.DevFlow.Agent), UITestForge connects to your app, taps and fills controls, navigates Shell routes, captures screenshots, and runs simple scripts you write yourself — no recompiling, no attaching a debugger, no writing UI test infrastructure.

> ⚠️ **Under construction** — APIs, script syntax, and UI are subject to change.

---

## Features

- **Live UI automation** — tap, fill, clear, and focus controls in a running app by `AutomationId`.
- **Visual tree inspector** — browse the live page/element tree of the target app to find automation IDs.
- **Script editor** — write, save, and run repeatable test scripts using a simple line-based DSL.
- **Flow control** — labels, `goto`, and conditional page checks (`checkpage` / `checknpage`) let you branch scripts based on the app's current state.
- **Script composition** — the `call` command lets you reuse scripts as building blocks for larger flows.
- **Screenshot capture** — grab screenshots at any point during a run for visual verification.
- **Shell navigation** — jump directly to any registered route.
- **Scroll support** — scroll the page by a pixel amount or scroll a specific element into view.
- **Automatic PowerPoint reporting** — generate a `.pptx` report from a script run with before/after screenshots and the full execution log via `create-pptx`.
- **Auto-refreshing page checks** — `checkpage` / `checknpage` always re-read the live visual tree before comparing, so checks reflect the app's true current state.
- **Snippet buttons** — quickly insert common command templates into the script editor.
- **Streaming execution log** — watch each script step run and report success/failure line by line.

## Screenshots

| Connect & inspect | Script editor help |
|---|---|
| ![UITestForge connected to a running app, showing the visual tree inspector](Screenshots/Screenshot%202026-08-27%20024507.png) | ![UITestForge script editor running a test script with a streaming execution log](Screenshots/Screenshot%202026-08-27%20024556.png) |

## How it works

UITestForge talks to your target app through the DevFlow broker/agent, then drives it using the same commands available from the CLI (`ui tap`, `ui fill`, `ui navigate`, etc.). Scripts in the editor are just a friendlier, file-based way to sequence those same commands.

## Getting Started

### 1. Add DevFlow to your MAUI app

**`.csproj`**
```xml
<PackageReference Include="Microsoft.Maui.DevFlow.Agent" Version="0.1.0-preview.12.26368.2" />
```

**`MauiProgram.cs`**
```csharp
#if DEBUG
using Microsoft.Maui.DevFlow.Agent;
#endif

// ...

#if DEBUG
builder.AddMauiDevFlowAgent();
#endif
```

### 2. Install the DevFlow CLI

```powershell
dotnet tool install -g Microsoft.Maui.Cli --prerelease
```

Reboot your machine after installing the tool for the first time.

### 3. Run your app and connect UITestForge

Launch your instrumented MAUI app in debug mode, then open UITestForge and connect to it. From there you can inspect the visual tree, run commands ad-hoc, or execute a script.

---

## Script Language

Scripts are plain text files (`.df`) made up of simple, line-based commands: `command [args]`. Blank lines and lines starting with `#` are ignored.

| Command | Description |
|---|---|
| `tap` | Tap a control by automation ID |
| `fill` | Enter text into a control |
| `clear` | Clear a control's text |
| `focus` | Give focus to a control |
| `screenshot` | Capture a screenshot |
| `navigate` | Navigate to a Shell route |
| `scroll` | Scroll the page or an element into view |
| `call` | Run another script file inline |
| `checkpage` | Branch if the current page matches |
| `checknpage` | Branch if the current page does *not* match |
| `goto` | Jump to a label |
| `wait` | Pause execution |
| `create-pptx` | Generate a PowerPoint report of the run |
| `exit` | Stop script execution |
| `# comment` | Ignored |
| `label:` | Defines a jump target for `goto` / `checkpage` / `checknpage` |

### `tap <automationId>`
Taps the control with the given `AutomationId`.
```
tap LoginBtn
```

### `fill <automationId> <text>`
Sets the text of an entry/editor control.
```
fill UsernameEntry admin@test.com
```

### `clear <automationId>`
Clears the text of a control.
```
clear UsernameEntry
```

### `focus <automationId>`
Gives keyboard focus to a control.
```
focus PasswordEntry
```

### `screenshot [path]`
Captures a screenshot. If no path is given, a temporary file is generated automatically.
```
screenshot
screenshot before-login.png
```

### `navigate <route>`
Navigates to a registered Shell route.
```
navigate //home
```

### `scroll down|up [px]`
Scrolls the page vertically by a pixel amount (default `300`).
```
scroll down
scroll up 500
```

### `scroll <automationId>`
Scrolls a specific element into view.
```
scroll SubmitBtn
```

### `call <script-filename>`
Executes another script file, then resumes with the next line of the current script.
```
call common_login.df
```

### `checkpage <pageName> <label>`
If the app's current page matches `pageName`, jumps to `label`.
```
checkpage SettingsPage onSettings
```

### `checknpage <pageName> <label>`
If the app's current page does **not** match `pageName`, jumps to `label`.
```
checknpage SettingsPage afterSettings
```

### `goto <label>`
Unconditionally jumps to a label defined elsewhere in the script.
```
goto retryLogin
```

### `wait <seconds>`
Pauses script execution for the given number of seconds.
```
wait 2
```

### `create-pptx [filename] [title]`
Generates a PowerPoint report from the run, combining the first and last screenshots captured, the execution log, and the script text into a single slide.
- If `filename` is omitted, a timestamped name is generated (`report_yyyyMMdd_HHmmss.pptx`).
- If `title` is omitted, it defaults to `"Test Report"`.
```
create-pptx
create-pptx my_test_report.pptx "Counter Button Test"
```

### `exit`
Stops script execution immediately.
```
exit
```

### Labels
A label is a line consisting only of a name followed by a colon. Used as a target for `goto`, `checkpage`, and `checknpage`.
```
retryLogin:
tap LoginBtn
```

### Comments
Lines starting with `#` (and blank lines) are ignored.
```
# This line does nothing
```

---

## Example: Login Flow

```
# Login flow
tap UsernameEntry
fill UsernameEntry admin@test.com
tap PasswordEntry
fill PasswordEntry secret123
tap LoginBtn
screenshot
```

## Example: Conditional Navigation with Auto-Refreshing Page Checks

```
screenshot start.png

# Navigate to a different page
tap NavigateToSettingsBtn
wait 2

# checkpage always re-reads the live page before comparing
checkpage SettingsPage onSettings

# Skipped if we jumped to onSettings
tap SomeOtherButton
exit

onSettings:
screenshot confirmed_on_settings.png
fill SettingEntry NewValue
tap BackButton
wait 2

checknpage SettingsPage afterSettings
tap AnotherSettingsButton
exit

afterSettings:
screenshot back_to_main.png
create-pptx auto_refresh_demo.pptx "Auto-Refresh Page Check Demo"
```

## Example: Reusable Login via `call`

```
call common_login.df
navigate //dashboard
screenshot dashboard.png
```

More runnable samples are available under [`UITestForge/SampleScripts`](UITestForge/SampleScripts).

---

## CLI Equivalents

Every script command maps to an equivalent DevFlow CLI invocation, so you can prototype commands directly from a terminal before adding them to a script:

```powershell
ui tap --automationId "LoginBtn"
ui fill --automationId "UsernameEntry" --text "admin@test.com"
ui clear --automationId "UsernameEntry"
ui focus --automationId "PasswordEntry"
ui screenshot --output "screenshot.png" --overwrite
ui navigate //home
ui scroll --dy 300
ui scroll --element "SubmitBtn"
```

---

## Contributing

Issues and pull requests are welcome! This project is under active development, so expect breaking changes to the script syntax and CLI as it matures.

## License

See [LICENSE](LICENSE) for details.
