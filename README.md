# UITestForge

<table>
<tr>
<td><img src="Doc/UITestForge_logo.png" alt="UITestForge logo" width="600"></td>
<td><b><i>Put your MAUI app on autopilot.</i></b></br></br>
UITestForge is a cross-platform <a href="https://learn.microsoft.com/dotnet/maui/">.NET MAUI</a> companion app that lets you drive, inspect, and test another running MAUI app in real time. Using <a href="https://www.nuget.org/packages/Microsoft.Maui.DevFlow.Agent">Microsoft.Maui.DevFlow.Agent</a>, UITestForge connects to your app, taps and fills controls, navigates Shell routes, captures screenshots, and runs simple scripts you write yourself — no recompiling, no attaching a debugger, no writing UI test infrastructure.</td>
</tr>
</table>

> ⚠️ **Under construction** — APIs, script syntax, and UI are subject to change.

---

## Table of Contents

- [Features](#features)
- [Screenshots](#screenshots)
- [How it works](#how-it-works)
- [Getting Started](#getting-started)
  - [1. Add DevFlow to your MAUI app](#1-add-devflow-to-your-maui-app)
  - [2. Install the DevFlow CLI](#2-install-the-devflow-cli)
  - [3. Run your app and connect UITestForge](#3-run-your-app-and-connect-uitestforge)
- [Script Language](#script-language)
  - [`tap <automationId>`](#tap-automationid)
  - [`fill <automationId> <text>`](#fill-automationid-text)
  - [`clear <automationId>`](#clear-automationid)
  - [`focus <automationId>`](#focus-automationid)
  - [`screenshot [path]`](#screenshot-path)
  - [`navigate <route>`](#navigate-route)
  - [`scroll down|up [px]`](#scroll-downup-px)
  - [`scroll <automationId>`](#scroll-automationid)
  - [`call <script-filename>`](#call-script-filename)
  - [`print <text>`](#print-text)
  - [`checkpage <pageName> [label]`](#checkpage-pagename-label)
  - [`checknpage <pageName> [label]`](#checknpage-pagename-label)
  - [`isvisible <automationId> [label]`](#isvisible-automationid-label)
  - [`isnvisible <automationId> [label]`](#isnvisible-automationid-label)
  - [`goto <label>`](#goto-label)
  - [`wait <seconds>`](#wait-seconds)
  - [`create-pptx [filename] [title]`](#create-pptx-filename-title)
  - [`exit`](#exit)
  - [Labels](#labels)
  - [Comments](#comments)
- [Example: Login Flow](#example-login-flow)
- [Example: Conditional Navigation with Auto-Refreshing Page Checks](#example-conditional-navigation-with-auto-refreshing-page-checks)
- [Example: Reusable Login via `call`](#example-reusable-login-via-call)
- [CLI Equivalents](#cli-equivalents)
- [Restrictions and next steps](#restrictions-and-next-steps)
- [Contributing](#contributing)
- [License](#license)

---

## Features

- **Live UI automation** — tap, fill, clear, and focus controls in a running app by `AutomationId`.
- **Visual tree inspector** — browse the live page/element tree of the target app to find automation IDs.
- **Script editor** — write, save, and run repeatable test scripts using a simple line-based DSL.
- **Syntax highlighting** — the editor colorizes commands, labels, `goto`/`call` targets, quoted arguments, numbers, and comments as you type.
- **Flow control** — labels, `goto`, and conditional page checks (`checkpage` / `checknpage`) let you branch scripts based on the app's current state.
- **Script composition** — the `call` command lets you reuse scripts as building blocks for larger flows.
- **Screenshot capture** — grab screenshots at any point during a run for visual verification.
- **Shell navigation** — jump directly to any registered route.
- **Scroll support** — scroll the page by a pixel amount or scroll a specific element into view.
- **Automatic PowerPoint reporting** — generate a `.pptx` report from a script run with before/after screenshots and the full execution log via `create-pptx`.
- **Auto-refreshing page checks** — `checkpage` / `checknpage` always re-read the live visual tree before comparing, so checks reflect the app's true current state.
- **Streaming execution log** — watch each script step run and report success/failure line by line.

## Screenshots

| Connect & inspect | Script editor help |
|---|---|
| ![UITestForge connected to a running app, showing the visual tree inspector](Screenshots/Screenshot%202026-08-27%20024507.png) | ![UITestForge script editor running a test script with a streaming execution log](Screenshots/Screenshot%202026-08-27%20024556.png) |

## How it works

UITestForge talks to your target app through the DevFlow broker/agent, then drives it using the same commands available from the CLI (`ui tap`, `ui fill`, `ui navigate`, etc.). Scripts in the editor are just a friendlier, file-based way to sequence those same commands.

<p align="center">
Human</br>
↓</br>
UITestForge UI</br>
↓</br>
Automation / Script Layer</br>
↓</br>
DevFlow</br>
↓</br>
Running MAUI Application</br>
<P>
&nbsp;

## Getting Started

### 1. Add DevFlow to your MAUI app

**Project file `.csproj`**
```xml
<PackageReference Include="Microsoft.Maui.DevFlow.Agent" Version="0.1.0-preview.12.26368.2" Condition="'$(Configuration)' == 'Debug'" />
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

The built-in script editor highlights this syntax while you type:

| Token | Styling |
|---|---|
| Known commands (`tap`, `fill`, `goto`, …) | Keyword color, **bold** |
| Unknown / misspelled commands | Underlined in the emphasis color, so typos stand out |
| Label definitions (`login:`) | Heading color, **bold** |
| `goto` / `call` targets | Link color, underlined |
| Quoted arguments (`"Counter Test Demo"`) | String color |
| Numeric arguments (`wait 2`, `scroll down 400`) | Number color |
| Comments (`# …`, including trailing comments) | Comment color, *italic* |

Colors follow the active editor theme (dark or light), so highlighting stays readable in both.

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
| `checkpage` | Check the current page (optionally branch if it matches) |
| `checknpage` | Check the current page (optionally branch if it does *not* match) |
| `goto` | Jump to a label |
| `wait` | Pause execution |
| `create-pptx` | Generate a PowerPoint report of the run |
| `add-report-page` | Add a before/log/after report page to the current PPTX |
| `addsummary` | Add an execution summary page (steps, pass/fail, duration, checked pages) to the current PPTX |
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

### `print <text>`
Writes a message to the execution log. Useful for adding notes or debugging context to a script run without performing any UI action.
```
print Starting login flow
```

### `checkpage <pageName> [label]`
If the app's current page matches `pageName`, jumps to `label`. If `label` is omitted, this just checks and records the page (no branching) — useful when you only want the page to show up in the `addsummary` report.
```
checkpage SettingsPage onSettings
checkpage SettingsPage
```

### `checknpage <pageName> [label]`
If the app's current page does **not** match `pageName`, jumps to `label`. If `label` is omitted, this just checks and records the page (no branching).
```
checknpage SettingsPage afterSettings
checknpage SettingsPage
```

### `isvisible <automationId> [label]`
If the element identified by `automationId` is currently visible, jumps to `label`. If `label` is omitted, this just checks the element's visibility (no branching). An element that cannot be found is treated as not visible.
```
isvisible SaveBtn onSaveVisible
isvisible SaveBtn
```

### `isnvisible <automationId> [label]`
If the element identified by `automationId` is **not** currently visible (or cannot be found), jumps to `label`. If `label` is omitted, this just checks the element's visibility (no branching).
```
isnvisible ErrorBanner onNoError
isnvisible ErrorBanner
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
A label is a line consisting only of a name followed by a colon. Used as a target for `goto`, `checkpage`, `checknpage`, `isvisible`, and `isnvisible`.
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

## Restrictions and next steps

- Communication with DevFlow is currently based on the CLI. Implementing a dedicated API is one of the next steps.
- A command-line version of UITestForge itself.
- Check and adapt UITestForge for macOS.
- Enhance the UI overall.
- *"Snippet buttons"* — quickly insert common command templates into the script editor.
- …

---

## Contributing

Issues and pull requests are welcome! This project is under active development, so expect breaking changes to the script syntax and CLI as it matures.

## License

See [LICENSE](LICENSE) for details.
