; SPDX-FileCopyrightText: None
; SPDX-License-Identifier: CC0-1.0

; -------------------------------
; Start
!include "MUI2.nsh"
!include "LogicLib.nsh"
!include "x64.nsh"

Unicode True
CRCCheck on

!define PRODUCT_NAME "tldr-sharp"
!define PRODUCT_DESCRIPTION "tldr-sharp Installer"
!define COPYRIGHT "Copyright © The tldr-sharp contributors"
!define PRODUCT_VERSION "VERSION_PLACEHOLDER"
!define SETUP_VERSION 1.0.0.0

!define MUI_BRANDINGTEXT "tldr-sharp ${PRODUCT_VERSION}"


;---------------------------------
;General

ShowInstDetails "hide"
ShowUninstDetails "hide"

;-------------------------------------------------------------------------------
; Attributes
Name "tldr-sharp"
OutFile "tldr-sharp_setup.exe"
!define PRODUCT_DIR_REGKEY "SOFTWARE\tldr-sharp"
!define PRODUCT_UNINST_KEY "SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\tldr-sharp"
InstallDirRegKey HKLM "${PRODUCT_DIR_REGKEY}" "InstallPath"
InstallDir "$PROGRAMFILES\${PRODUCT_NAME}"
RequestExecutionLevel admin

;-------------------------------------------------------------------------------
; Version Info
VIProductVersion "${PRODUCT_VERSION}"
VIAddVersionKey "ProductName" "${PRODUCT_NAME}"
VIAddVersionKey "ProductVersion" "${PRODUCT_VERSION}"
VIAddVersionKey "FileDescription" "${PRODUCT_DESCRIPTION}"
VIAddVersionKey "LegalCopyright" "${COPYRIGHT}"
VIAddVersionKey "FileVersion" "${SETUP_VERSION}"

;-------------------------------------------------------------------------------
; Modern UI Appearance
!define MUI_ICON "${NSISDIR}\Contrib\Graphics\Icons\nsis3-install.ico"
!define MUI_HEADERIMAGE
!define MUI_HEADERIMAGE_BITMAP "${NSISDIR}\Contrib\Graphics\Header\nsis3-metro.bmp"
!define MUI_WELCOMEFINISHPAGE_BITMAP "${NSISDIR}\Contrib\Graphics\Wizard\nsis3-metro.bmp"
!define MUI_UNWELCOMEFINISHPAGE_BITMAP "${NSISDIR}\Contrib\Graphics\Wizard\nsis3-metro.bmp"
!define MUI_ABORTWARNING
!define MUI_FINISHPAGE_NOAUTOCLOSE

;-------------------------------------------------------------------------------
; Installer Pages
!insertmacro MUI_PAGE_WELCOME
!insertmacro MUI_PAGE_COMPONENTS
!insertmacro MUI_PAGE_DIRECTORY
!insertmacro MUI_PAGE_INSTFILES
!insertmacro MUI_PAGE_FINISH

;-------------------------------------------------------------------------------
; Uninstaller Pages
!insertmacro MUI_UNPAGE_WELCOME
!insertmacro MUI_UNPAGE_CONFIRM
!insertmacro MUI_UNPAGE_INSTFILES
!insertmacro MUI_UNPAGE_FINISH

;-------------------------------------------------------------------------------
; Languages
!insertmacro MUI_LANGUAGE "English"

LangString DESC_install ${LANG_ENGLISH} "Install tldr-sharp"
LangString DESC_path ${LANG_ENGLISH} "Add tldr-sharp to the system Path. This allows calling tldr in the terminal without specifying the full path to the tldr-sharp executable."

;--------------------------------
;Installer Sections
Section "tldr-sharp" SectionInstall
    SectionIn RO

    SetOutPath "$INSTDIR"
    File /r "release\*.*"

    WriteRegStr HKLM "${PRODUCT_DIR_REGKEY}" "InstallPath" "$INSTDIR"
    WriteRegStr HKLM "${PRODUCT_DIR_REGKEY}" "Version" "${PRODUCT_VERSION}"
    WriteRegStr HKLM "${PRODUCT_UNINST_KEY}" "DisplayName" "${PRODUCT_NAME}"
    WriteRegStr HKLM "${PRODUCT_UNINST_KEY}" "DisplayVersion" "${PRODUCT_VERSION}"
    WriteRegStr HKLM "${PRODUCT_UNINST_KEY}" "URLInfoAbout" "https://github.com/principis/tldr-sharp/"
    WriteRegDWORD HKLM "${PRODUCT_UNINST_KEY}" "NoModify" 1
    WriteRegDWORD HKLM "${PRODUCT_UNINST_KEY}" "NoRepair" 1
    WriteRegStr HKLM "${PRODUCT_UNINST_KEY}" "UninstallString" "$INSTDIR\uninstall.exe"
    WriteUninstaller "$INSTDIR\Uninstall.exe"


SectionEnd


Section "Add to Path" SectionSystem
    SectionIn 1

    Var /GLOBAL writeCode
    Var /GLOBAL existsCode
    Var /GLOBAL addCode

    ; Set to HKLM
    EnVar::SetHKLM
    EnVar::Check "NULL" "NULL"
    Pop $writeCode

    EnVar::Check "path" "NULL"
    Pop $existsCode

    ; Add to path
    EnVar::AddValue "path" "$INSTDIR"
    Pop $addCode

    ${If} $addCode == 0
        DetailPrint "Added to Path: $INSTDIR"
    ${Else}
        DetailPrint "Error: Failed to add to Path: $INSTDIR"
        DetailPrint "Error: EnVar::Check write access HKLM returned=$writeCode"
        DetailPrint "Error: EnVar::Check Path existence returned=$existsCode"
        DetailPrint "Error: EnVar::AddValue Add Path returned=$addCode"
    ${EndIf}

SectionEnd

; Section descriptions
!insertmacro MUI_FUNCTION_DESCRIPTION_BEGIN
!insertmacro MUI_DESCRIPTION_TEXT ${SectionInstall} $(DESC_install)
!insertmacro MUI_DESCRIPTION_TEXT ${SectionSystem} $(DESC_path)
!insertmacro MUI_FUNCTION_DESCRIPTION_END


;--------------------------------
;Uninstaller Section
Section "Uninstall" Uninstall

    ;Delete Files
    RMDir /r "$INSTDIR\*.*"

    ;Remove the installation directory
    RMDir "$INSTDIR"

    ; Set to HKLM
    EnVar::SetHKLM

    ; Delete from path
    EnVar::DeleteValue "path" "$INSTDIR"
    Pop $0

    ${If} $0 == 0
            DetailPrint "Removed from Path: $INSTDIR"
    ${Else}
        DetailPrint "Failed to remove from Path: $INSTDIR"
    ${EndIf}

    DeleteRegKey HKLM "${PRODUCT_DIR_REGKEY}"
    DeleteRegKey HKLM "${PRODUCT_UNINST_KEY}"

SectionEnd

;eof
