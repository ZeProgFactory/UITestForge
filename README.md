# UITestForge
Put your MAUI app on autopilot

# !!! under construction !!!

# DevFlow
https://mauidevflow.net/
https://learn.microsoft.com/en-us/dotnet/maui/developer-tools/?view=net-maui-10.0
https://github.com/dotnet/maui-labs/tree/main/src/Cli 
https://learn.microsoft.com/en-us/dotnet/maui/developer-tools/cli/?view=net-maui-10.0


<PackageReference Include="Microsoft.Maui.DevFlow.Agent" Version="0.1.0-preview.12.26368.2" />

MauiProgram.cs

#if DEBUG
using Microsoft.Maui.DevFlow.Agent;
#endif

#if DEBUG
builder.AddMauiDevFlowAgent();
#endif


dotnet tool install -g Microsoft.Maui.Cli --prerelease

maui devflow broker start
maui devflow wait

maui devflow ui tree --depth 3 --fields "id,type,text,automationId"

maui devflow ui tap --automationId "CounterBtn"
