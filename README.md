# UITestForge
Put your MAUI app on autopilot

# !!! under construction !!!

# DevFlow
https://mauidevflow.net/
https://learn.microsoft.com/en-us/dotnet/maui/developer-tools/?view=net-maui-10.0
https://github.com/dotnet/maui-labs/tree/main/src/Cli 
https://learn.microsoft.com/en-us/dotnet/maui/developer-tools/cli/?view=net-maui-10.0
https://dotnet.microsoft.com/en-us/live/community-standup


<PackageReference Include="Microsoft.Maui.DevFlow.Agent" Version="0.1.0-preview.12.26368.2" />

MauiProgram.cs

#if DEBUG
using Microsoft.Maui.DevFlow.Agent;
#endif

#if DEBUG
builder.AddMauiDevFlowAgent();
#endif


dotnet tool install -g Microsoft.Maui.Cli --prerelease
reboot

dotnet tool uninstall Microsoft.Maui.Cli --global

maui devflow broker start
maui devflow wait

maui devflow ui tree --depth 3 --fields "id,type,text,automationId"

maui devflow ui tap --automationId "CounterBtn"


https://github.com/dotnet/maui-labs/issues/408
"C:\Program Files (x86)\Android\android-sdk\platform-tools\adb.exe"  reverse --remove-all
"C:\Program Files (x86)\Android\android-sdk\platform-tools\adb.exe"  forward  --remove-all
maui devflow init


# Login flow
tap UsernameEntry
fill UsernameEntry admin@test.com
tap PasswordEntry
fill PasswordEntry secret123
tap LoginBtn
screenshot


# Script Editor 

## Syntax
- Script syntax is simple line-based: `command [args]`  
  - `tap <automationId>`  
  - `fill <automationId> <text>`  
  - `clear <automationId>`  
  - `focus <automationId>`  
  - `screenshot [optional-path]`  
  - `navigate <route>` — navigate to a Shell route (e.g. `navigate //home`)  
  - `scroll down [px]` — scroll down by px (default 300)  
  - `scroll up [px]` — scroll up by px (default 300)  
  - `scroll <automationId>` — scroll element into view  
  - `call <script-filename>` — executes the script file before resuming with the next line of the current script
  - `checkpage <pageName> <label>` — if current page is pageName, goto label
  - `checknpage <pageName> <label>` — if current page isn't pageName, goto label
  - `goto <label>` — jump to a label in the script
  - `wait <seconds>` — pause execution for the specified number of seconds
  - `exit` — stop script execution
  - `# comment` / blank lines skipped  
- CLI commands follow existing patterns: `ui tap --automationId "id"`, `ui fill --automationId "id" --text "value"`, `ui clear --automationId "id"`, `ui focus --automationId "id"`, `ui screenshot --output "path" --overwrite`, `ui navigate <route>`, `ui scroll --dy <px>`, `ui scroll --element "id"`
- Snippet buttons append template text to the Editor
- Run executes line by line, streaming output to a result log area
- `#if !ANDROID` guard used like the rest of the file
