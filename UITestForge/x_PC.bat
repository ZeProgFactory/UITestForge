@rem dotnet workload update
cls
call ..\Clean.bat
dotnet clean
..\UpdateVersionInfoMaui -s -i -ui

dotnet build UITestForge.csproj -f net10.0-windows10.0.19041.0 -c Release /p:Platform="AnyCPU" 
@echo off
@if %ERRORLEVEL% neq 0 (
    @echo *** Publish failed with error level %ERRORLEVEL% ***
    @exit /b %ERRORLEVEL%
)    

"C:\Program Files (x86)\NSIS\makensis.exe" UITestForge.nsi
@if %ERRORLEVEL% neq 0 (
    @echo *** Publish failed with error level %ERRORLEVEL% ***
    @exit /b %ERRORLEVEL%
)   

@echo *** NSI succeeded *** 

copy /y "D:\GitWare\Apps\UITestForge\UITestForge\LastUpdate.json" "E:\OneDrive - ZPF\_Share_\ZPF\UITestForge\UITestForge.Install.LastUpdate.json"

@rem - - - make a copie from latest install to "xxx.Install.LastUpdate.exe" - - -"
@echo off
setlocal

set "sourceDir=E:\OneDrive - ZPF\_Share_\ZPF\UITestForge\"
set "destinationDir=E:\OneDrive - ZPF\_Share_\ZPF\UITestForge\"
set "newestFile="

for /f "delims=" %%F in ('dir /b /a:-d /o-d "%sourceDir%\*.exe"') do (
    set "newestFile=%%F"
    goto :found
)

:found
if defined newestFile (
    copy "%sourceDir%\%newestFile%" "%destinationDir%UITestForge.Install.LastUpdate.exe"
    echo Copied newest NSIS installer: %newestFile%
) else (
    echo No .exe files found in %sourceDir%
)

endlocal

@echo *** rename on E:\ *** 

@rem call scs LastUpdate.Win.scs