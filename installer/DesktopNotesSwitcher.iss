#ifndef AppVersion
  #error Pass the published application version with /DAppVersion.
#endif
#ifndef Payload
  #error Pass the publish folder with /DPayload.
#endif

#define AppName "桌面便笺"
#define AppExe "DesktopNotesSwitcher.exe"
#define StartupKey "Software\Microsoft\Windows\CurrentVersion\Run"
#define StartupValue "DesktopNotesSwitcher"

[Setup]
AppId={{75BF8684-19DF-4BB4-A732-A531DB7E0C8B}
AppName={#AppName}
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
OutputBaseFilename=DesktopNotesSwitcher-v{#AppVersion}-Setup-win-x64
Compression=lzma2
SolidCompression=yes
WizardStyle=modern

[Languages]
Name: "chs"; MessagesFile: "compiler:Languages\ChineseSimplified.isl"
Name: "en"; MessagesFile: "compiler:Default.isl"

[Files]
Source: "{#Payload}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{autoprograms}\{#AppName}"; Filename: "{app}\{#AppExe}"; WorkingDir: "{app}"

[Run]
Filename: "{app}\{#AppExe}"; Description: "{cm:LaunchProgram,{#AppName}}"; Flags: nowait postinstall skipifsilent

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
