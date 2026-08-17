; OpenMultiSeat Installer Script
; NSIS Installer for Windows multi-seat system

!include "MUI2.nsh"
!include "x64.nsh"
!include "LogicLib.nsh"

; Product Information
!define PRODUCT_NAME "OpenMultiSeat"
!define PRODUCT_VERSION "1.0.0"
!define PRODUCT_PUBLISHER "OpenMultiSeat Contributors"
!define PRODUCT_URL "https://github.com/RKY2023/OpenMultiSeat"
!define PRODUCT_DIR_REGKEY "Software\Microsoft\Windows\CurrentVersion\App Paths\OpenMultiSeat.exe"
!define PRODUCT_UNINST_KEY "Software\Microsoft\Windows\CurrentVersion\Uninstall\${PRODUCT_NAME}"
!define PRODUCT_UNINST_ROOT_KEY "HKLM"

; Installation Directory
InstallDir "$PROGRAMFILES64\${PRODUCT_NAME}"

; Installer Attributes
Name "${PRODUCT_NAME} ${PRODUCT_VERSION}"
OutFile "..\..\build\installer\OpenMultiSeat-${PRODUCT_VERSION}-x64-setup.exe"
ShowInstDetails show
ShowUnInstDetails show

; MUI Settings
!insertmacro MUI_PAGE_WELCOME
!insertmacro MUI_PAGE_LICENSE "..\..\LICENSE"
!insertmacro MUI_PAGE_COMPONENTS
!insertmacro MUI_PAGE_DIRECTORY
!insertmacro MUI_PAGE_INSTFILES
!insertmacro MUI_PAGE_FINISH

!insertmacro MUI_UNPAGE_CONFIRM
!insertmacro MUI_UNPAGE_INSTFILES

!insertmacro MUI_LANGUAGE "English"

; Installation Sections

Section "Core Components" SEC01
  SetOutPath "$INSTDIR\bin"
  SetOverwrite try

  File "..\..\src\OpenMultiSeat.Core\bin\x64\Release\net9.0-windows\OpenMultiSeat.Core.dll"
  File "..\..\src\OpenMultiSeat.Devices\bin\x64\Release\net9.0-windows\OpenMultiSeat.Devices.dll"
  File "..\..\src\OpenMultiSeat.Sessions\bin\x64\Release\net9.0-windows\OpenMultiSeat.Sessions.dll"
  File "..\..\src\OpenMultiSeat.Displays\bin\x64\Release\net9.0-windows\OpenMultiSeat.Displays.dll"
  File "..\..\src\OpenMultiSeat.InputIsolation\bin\x64\Release\net9.0-windows\OpenMultiSeat.InputIsolation.dll"
  File "..\..\src\OpenMultiSeat.Audio\bin\x64\Release\net9.0-windows\OpenMultiSeat.Audio.dll"
  File "..\..\src\OpenMultiSeat.IPC\bin\x64\Release\net9.0-windows\OpenMultiSeat.IPC.dll"
  File "..\..\src\OpenMultiSeat.Service\bin\x64\Release\net9.0-windows\OpenMultiSeat.Service.exe"

  SetOutPath "$INSTDIR\config"
  FileOpen $0 "$INSTDIR\config\.gitkeep" w
  FileClose $0

  WriteRegStr HKLM "${PRODUCT_DIR_REGKEY}" "" "$INSTDIR\bin\OpenMultiSeat.Service.exe"
SectionEnd

Section "GUI Application" SEC02
  SetOutPath "$INSTDIR\bin"
  SetOverwrite try

  File "..\..\src\OpenMultiSeat.GUI\bin\x64\Release\net9.0-windows\OpenMultiSeat.GUI.exe"

  SetOutPath "$SMPROGRAMS\${PRODUCT_NAME}"
  CreateShortCut "$SMPROGRAMS\${PRODUCT_NAME}\${PRODUCT_NAME} Admin.lnk" "$INSTDIR\bin\OpenMultiSeat.GUI.exe"

  SetOutPath "$DESKTOP"
  CreateShortCut "$DESKTOP\${PRODUCT_NAME} Admin.lnk" "$INSTDIR\bin\OpenMultiSeat.GUI.exe"
SectionEnd

Section "Testing Tools" SEC03
  SetOutPath "$INSTDIR\tools"
  SetOverwrite try

  File "..\..\scripts\bin\x64\Release\net9.0-windows\Phase0.Poc.exe"
  File "..\..\scripts\bin\x64\Release\net9.0-windows\Phase1.DeviceTester.exe"
  File "..\..\scripts\bin\x64\Release\net9.0-windows\Phase2.SeatConfigurator.exe"
  File "..\..\scripts\bin\x64\Release\net9.0-windows\Phase3.SessionTester.exe"
  File "..\..\scripts\bin\x64\Release\net9.0-windows\Phase4.DisplayConfigurator.exe"
  File "..\..\scripts\bin\x64\Release\net9.0-windows\Phase5.InputIsolationTester.exe"
  File "..\..\scripts\bin\x64\Release\net9.0-windows\Phase6.AudioConfigurator.exe"

  SetOutPath "$SMPROGRAMS\${PRODUCT_NAME}\Tools"
  CreateShortCut "$SMPROGRAMS\${PRODUCT_NAME}\Tools\Device Tester.lnk" "$INSTDIR\tools\Phase1.DeviceTester.exe"
  CreateShortCut "$SMPROGRAMS\${PRODUCT_NAME}\Tools\Seat Configurator.lnk" "$INSTDIR\tools\Phase2.SeatConfigurator.exe"
  CreateShortCut "$SMPROGRAMS\${PRODUCT_NAME}\Tools\Display Configurator.lnk" "$INSTDIR\tools\Phase4.DisplayConfigurator.exe"
  CreateShortCut "$SMPROGRAMS\${PRODUCT_NAME}\Tools\Input Isolation Tester.lnk" "$INSTDIR\tools\Phase5.InputIsolationTester.exe"
  CreateShortCut "$SMPROGRAMS\${PRODUCT_NAME}\Tools\Audio Configurator.lnk" "$INSTDIR\tools\Phase6.AudioConfigurator.exe"
SectionEnd

Section "Documentation" SEC04
  SetOutPath "$INSTDIR\docs"
  SetOverwrite try

  File "..\..\README.md"
  File "..\..\LICENSE"
  File "..\..\CHANGELOG.md"
SectionEnd

Section -AdditionalIcons
  SetOutPath "$SMPROGRAMS\${PRODUCT_NAME}"
  CreateShortCut "$SMPROGRAMS\${PRODUCT_NAME}\Uninstall.lnk" "$INSTDIR\uninst.exe"
SectionEnd

Section -Post
  WriteUninstaller "$INSTDIR\uninst.exe"
  WriteRegStr HKLM "${PRODUCT_UNINST_KEY}" "DisplayName" "$(^Name)"
  WriteRegStr HKLM "${PRODUCT_UNINST_KEY}" "UninstallString" "$INSTDIR\uninst.exe"
  WriteRegStr HKLM "${PRODUCT_UNINST_KEY}" "DisplayVersion" "${PRODUCT_VERSION}"
  WriteRegStr HKLM "${PRODUCT_UNINST_KEY}" "URLInfoAbout" "${PRODUCT_URL}"
  WriteRegStr HKLM "${PRODUCT_UNINST_KEY}" "Publisher" "${PRODUCT_PUBLISHER}"
SectionEnd

; Uninstaller Section
Section Uninstall
  RMDir /r "$SMPROGRAMS\${PRODUCT_NAME}"
  Delete "$DESKTOP\${PRODUCT_NAME} Admin.lnk"

  RMDir /r "$INSTDIR\bin"
  RMDir /r "$INSTDIR\tools"
  RMDir /r "$INSTDIR\docs"
  RMDir /r "$INSTDIR\config"
  RMDir "$INSTDIR"

  DeleteRegKey HKLM "${PRODUCT_DIR_REGKEY}"
  DeleteRegKey HKLM "${PRODUCT_UNINST_KEY}"
SectionEnd

; Section Descriptions
LangString DESC_SEC01 ${LANG_ENGLISH} "Core system components (services, libraries, configuration storage)"
LangString DESC_SEC02 ${LANG_ENGLISH} "WPF administration console for system configuration"
LangString DESC_SEC03 ${LANG_ENGLISH} "Interactive testing and configuration tools for each phase"
LangString DESC_SEC04 ${LANG_ENGLISH} "Documentation, README, and license files"

!insertmacro MUI_FUNCTION_DESCRIPTION_BEGIN
!insertmacro MUI_DESCRIPTION_TEXT ${SEC01} $(DESC_SEC01)
!insertmacro MUI_DESCRIPTION_TEXT ${SEC02} $(DESC_SEC02)
!insertmacro MUI_DESCRIPTION_TEXT ${SEC03} $(DESC_SEC03)
!insertmacro MUI_DESCRIPTION_TEXT ${SEC04} $(DESC_SEC04)
!insertmacro MUI_FUNCTION_DESCRIPTION_END

; Function Callbacks
Function .onInit
  ${If} ${RunningX64}
    DetailPrint "Detected 64-bit Windows"
  ${Else}
    MessageBox MB_OK "OpenMultiSeat requires 64-bit Windows"
    Abort
  ${EndIf}
FunctionEnd

Function un.onInit
  MessageBox MB_ICONQUESTION|MB_YESNO|MB_DEFBUTTON2 "Remove ${PRODUCT_NAME} and all its components?" IDYES +2
  Abort
FunctionEnd
