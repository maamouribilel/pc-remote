#ifndef AppVersion
  #define AppVersion "0.4.1"
#endif
[Setup]
AppId={{DDF99789-80D2-481E-9501-9723169F070E}
AppName=PC Remote
AppVersion={#AppVersion}
AppPublisher=PC Remote project
DefaultDirName={localappdata}\Programs\PCRemote
DefaultGroupName=PC Remote
DisableDirPage=yes
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0.17763
OutputDir={#ReleaseDir}
OutputBaseFilename=PCRemote-{#AppVersion}-win-x64-setup
SetupIconFile={#PublishDir}\wwwroot\pc-remote-v2.ico
UninstallDisplayIcon={app}\PcController.App.exe
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
CloseApplications=no
RestartApplications=no
UninstallLogMode=append

[Tasks]
Name: "firewall"; Description: "Configure HTTPS access from the Private local network (Windows administrator prompt)"; Flags: checkedonce

[Files]
Source: "{#PublishDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\PC Remote"; Filename: "{app}\PcController.App.exe"
Name: "{group}\Uninstall PC Remote"; Filename: "{uninstallexe}"

[Run]
Filename: "{app}\PcController.App.exe"; Parameters: "--firewall-install"; Tasks: firewall; Check: not WizardSilent; Flags: waituntilterminated
Filename: "{app}\PcController.App.exe"; Description: "Open PC Remote setup"; Flags: nowait postinstall skipifsilent

[Code]
function PrepareToInstall(var NeedsRestart: Boolean): String;
var ResultCode: Integer;
begin
  Result := '';
  if FileExists(ExpandConstant('{app}\PcController.App.exe')) then begin
    if not Exec(ExpandConstant('{app}\PcController.App.exe'), '--exit', '', SW_HIDE, ewWaitUntilTerminated, ResultCode) or (ResultCode <> 0) then
      Result := 'Exit PC Remote using its notification tray menu, then try again.';
  end;
end;

function InitializeUninstall(): Boolean;
var ResultCode: Integer; Startup: String;
begin
  Result := False;
  if FileExists(ExpandConstant('{app}\PcController.App.exe')) then begin
    if not Exec(ExpandConstant('{app}\PcController.App.exe'), '--exit', '', SW_HIDE, ewWaitUntilTerminated, ResultCode) or (ResultCode <> 0) then begin
      MsgBox('Exit PC Remote from its notification tray and retry uninstall.', mbError, MB_OK); Exit;
    end;
    if not Exec(ExpandConstant('{app}\PcController.App.exe'), '--firewall-remove', '', SW_HIDE, ewWaitUntilTerminated, ResultCode) or (ResultCode <> 0) then begin
      MsgBox('The application-owned firewall rule could not be removed. Approve the Windows administrator prompt and retry uninstall. Application files were retained.', mbError, MB_OK); Exit;
    end;
  end;
  if RegQueryStringValue(HKCU, 'Software\Microsoft\Windows\CurrentVersion\Run', 'PcController', Startup) then
    if CompareText(Startup, '"' + ExpandConstant('{app}\PcController.App.exe') + '" --background') = 0 then
      RegDeleteValue(HKCU, 'Software\Microsoft\Windows\CurrentVersion\Run', 'PcController');
  Result := True;
end;

procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
begin
  if (CurUninstallStep = usPostUninstall) and not UninstallSilent then
    MsgBox('PC Remote is removed. Its protected certificates/devices were retained for reinstall. To remove trust, delete PC Remote Local CA from iPhone Settings > General > VPN & Device Management, remove its Home Screen icon, and see CERTIFICATES.md from the release archive for Windows trust/data cleanup.', mbInformation, MB_OK);
end;
