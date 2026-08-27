!include "MUI.nsh"
!define MUI_DIRECTORYPAGE_VARIABLE $INSTDIR
!insertmacro MUI_PAGE_DIRECTORY
!insertmacro MUI_PAGE_INSTFILES

!define MUI_FINISHPAGE_RUN
!define MUI_FINISHPAGE_RUN_FUNCTION "LaunchLink"
!insertmacro MUI_PAGE_FINISH

!insertmacro MUI_LANGUAGE "English"

; run without admin privileges (else admin )
RequestExecutionLevel user

;General
Name "UITestForge"
!define FriendlyAppName "UITestForge"
!define AppName "UITestForge"
!define /date DATE "%Y%m%d"

OutFile "E:\OneDrive - ZPF\_Share_\ZPF\${FriendlyAppName}\${FriendlyAppName}.Install.${DATE}.exe"
!define SourceFiles "D:\GitWare\Apps\UITestForge\UITestForge\bin\Release\net10.0-windows10.0.19041.0\win-x64"
icon "D:\GitWare\Apps\UITestForge\UITestForge\bin\Release\net10.0-windows10.0.19041.0\win-x64\icon.ico"

; Show install details
; ShowInstDetails show
 
;Folder selection page
InstallDir "$PROGRAMFILES\${FriendlyAppName}"

# default section start; every NSIS script has at least one section.
Section
 
# define the output path for this file
SetOutPath $INSTDIR
File /r "${SourceFiles}\*.*"

; Create application shortcut (first in installation dir to have the correct "start in" target)
SetOutPath "$INSTDIR"
CreateShortCut "$INSTDIR\${FriendlyAppName}.lnk" "$INSTDIR\${AppName}.exe"
CreateShortcut "$DESKTOP\${FriendlyAppName}.lnk" "$INSTDIR\${AppName}.exe" "" 

; Start menu entries
SetOutPath "$SMPROGRAMS\${AppName}\"
CopyFiles "$INSTDIR\${FriendlyAppName}.lnk" "$SMPROGRAMS\${FriendlyAppName}\"
Delete "$INSTDIR\${FriendlyAppName}.lnk"

SetOutPath $INSTDIR

WriteUninstaller $INSTDIR\uninstall.exe

# default section end
SectionEnd

Section "Uninstall"

RMDir /r /REBOOTOK $INSTDIR
RMDir /r /REBOOTOK "$SMPROGRAMS\${FriendlyAppName}\"

SectionEnd

Function .onInit
StrCpy $INSTDIR "C:\Apps\${FriendlyAppName}"
FunctionEnd

Function LaunchLink
;ExecShell "" "$DESKTOP\${FriendlyAppName}.lnk"
ExecShell "" '"$INSTDIR\${AppName}.exe"'
FunctionEnd
