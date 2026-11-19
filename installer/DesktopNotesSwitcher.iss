#ifndef AppVersion
  #error Pass the published application version with /DAppVersion.
#endif
#ifndef Payload
  #error Pass the publish folder with /DPayload.
#endif

#define AppExe "DesktopNotesSwitcher.exe"
#define StartupKey "Software\Microsoft\Windows\CurrentVersion\Run"
#define StartupValue "DesktopNotesSwitcher"

[Setup]
AppId={{75BF8684-19DF-4BB4-A732-A531DB7E0C8B}
AppName={cm:ProductName}
AppVerName={cm:ProductName} {#AppVersion}
AppVersion={#AppVersion}
AppPublisher=numb-ys
AppPublisherURL=https://github.com/numb-ys/desktop-notes-switcher
AppSupportURL=https://github.com/numb-ys/desktop-notes-switcher/issues
DefaultDirName={localappdata}\Programs\DesktopNotesSwitcher
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
MinVersion=10.0.26100
ArchitecturesAllowed=x64os
ArchitecturesInstallIn64BitMode=x64os
AppMutex=Local\DesktopNotesSwitcher
SetupIconFile=..\assets\notes.ico
UninstallDisplayIcon={app}\{#AppExe}
VersionInfoVersion={#AppVersion}
VersionInfoDescription=Desktop Notes Switcher Setup
VersionInfoProductName=Desktop Notes Switcher
OutputBaseFilename=DesktopNotesSwitcher-Installer
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
ShowLanguageDialog=no

[Languages]
Name: "chs"; MessagesFile: "compiler:Languages\ChineseSimplified.isl"
Name: "en"; MessagesFile: "compiler:Default.isl"

[CustomMessages]
chs.ProductName=桌面便笺
en.ProductName=Desktop Notes Switcher

[Files]
Source: "{#Payload}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "languages\zh-CN.txt"; DestDir: "{app}"; DestName: "ui-language.txt"; Languages: chs; Flags: ignoreversion
Source: "languages\en.txt"; DestDir: "{app}"; DestName: "ui-language.txt"; Languages: en; Flags: ignoreversion

[InstallDelete]
Type: files; Name: "{autoprograms}\桌面便笺.lnk"; Languages: en
Type: files; Name: "{autoprograms}\Desktop Notes Switcher.lnk"; Languages: chs

[Icons]
Name: "{autoprograms}\{cm:ProductName}"; Filename: "{app}\{#AppExe}"; WorkingDir: "{app}"

[Run]
Filename: "{app}\{#AppExe}"; Description: "{cm:LaunchProgram,{cm:ProductName}}"; Flags: nowait postinstall skipifsilent

[Code]
procedure CurUninstallStepChanged(Step: TUninstallStep);
var
  Command: String;
  InstalledCommand: String;
begin
  if Step = usUninstall then
  begin
    InstalledCommand := '"' + ExpandConstant('{app}\{#AppExe}') + '"';
    if RegQueryStringValue(HKCU, '{#StartupKey}', '{#StartupValue}', Command)
      and (CompareText(Command, InstalledCommand) = 0) then
    begin
      if not RegDeleteValue(HKCU, '{#StartupKey}', '{#StartupValue}') then
        Log('The application startup entry could not be removed.');
    end;
  end;
end;
