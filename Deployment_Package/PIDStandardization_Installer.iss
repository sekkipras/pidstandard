; -- PIDStandardization_Installer.iss --
; Inno Setup Script for P&ID Standardization Application
; Download Inno Setup from: https://jrsoftware.org/isdl.php

#define MyAppName "P&ID Standardization"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "P&ID Standardization Team"
#define MyAppURL "https://pidstandardization.local"
#define MyAppExeName "PIDStandardization.UI.exe"

[Setup]
; Basic Information
AppId={{A1B2C3D4-E5F6-7890-ABCD-EF1234567890}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={autopf}\PIDStandardization
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
LicenseFile=
OutputDir=Installer_Output
OutputBaseFilename=PIDStandardization_Setup_v{#MyAppVersion}
SetupIconFile=
Compression=lzma
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=admin
ArchitecturesAllowed=x64
ArchitecturesInstallIn64BitMode=x64

; Visual Style - Using default images
; WizardImageFile=compiler:WizModernImage-IS.bmp
; WizardSmallImageFile=compiler:WizModernSmallImage-IS.bmp

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked
Name: "autocadautoload"; Description: "Configure AutoCAD 2026 to auto-load plugin on startup"; GroupDescription: "AutoCAD Integration:"; Flags: checkedonce

[Files]
; Main Application
Source: "PIDStandardization.UI.exe"; DestDir: "{app}"; Flags: ignoreversion

; AutoCAD Plugin Files
Source: "AutoCAD_Plugin\*"; DestDir: "{app}\AutoCAD_Plugin"; Flags: ignoreversion recursesubdirs createallsubdirs

; Documentation
Source: "README.txt"; DestDir: "{app}"; Flags: ignoreversion isreadme
Source: "INSTALLATION_GUIDE.txt"; DestDir: "{app}"; Flags: ignoreversion

; Templates folder (for future use) - commented out until templates are added
; Source: "Templates\*"; DestDir: "{app}\Templates"; Flags: ignoreversion recursesubdirs createallsubdirs; Permissions: users-full

[Icons]
; Start Menu shortcuts
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\AutoCAD Plugin Installation Guide"; Filename: "{app}\AutoCAD_Plugin\INSTALLATION_INSTRUCTIONS.txt"
Name: "{group}\User Guide"; Filename: "{app}\INSTALLATION_GUIDE.txt"
Name: "{group}\{cm:UninstallProgram,{#MyAppName}}"; Filename: "{uninstallexe}"

; Desktop shortcut (optional)
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
; Option to launch application after installation
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

[Code]
var
  AutoCADConfigPage: TInputOptionWizardPage;

procedure InitializeWizard;
begin
  // Create custom page for AutoCAD configuration
  AutoCADConfigPage := CreateInputOptionPage(wpSelectTasks,
    'AutoCAD Plugin Configuration',
    'Configure AutoCAD to automatically load the P&ID plugin',
    'The installer can configure AutoCAD 2026 to automatically load the plugin on startup. ' +
    'If AutoCAD is not installed or you prefer to load the plugin manually, you can skip this step.',
    True, False);

  AutoCADConfigPage.Add('Configure AutoCAD 2026 auto-load (Recommended)');
  AutoCADConfigPage.Add('I will configure AutoCAD manually later');

  AutoCADConfigPage.Values[0] := True;
end;

procedure CurStepChanged(CurStep: TSetupStep);
var
  AutoCADSupportPath: String;
  LspFilePath: String;
  LspContent: String;
  ResultCode: Integer;
begin
  if CurStep = ssPostInstall then
  begin
    // Configure AutoCAD auto-load if selected
    if AutoCADConfigPage.Values[0] then
    begin
      AutoCADSupportPath := ExpandConstant('{userappdata}\Autodesk\AutoCAD 2026\R24.1\enu\Support');

      // Check if AutoCAD support directory exists
      if DirExists(AutoCADSupportPath) then
      begin
        LspFilePath := AutoCADSupportPath + '\acad2026.lsp';

        // Create LSP file content
        LspContent := '; P&ID Standardization Plugin Auto-load' + #13#10 +
                      '; This file automatically loads the PIDStandardization plugin when AutoCAD starts' + #13#10 +
                      '(defun S::STARTUP ()' + #13#10 +
                      '  (command "._NETLOAD" "' + ExpandConstant('{app}') + '\AutoCAD_Plugin\PIDStandardization.AutoCAD.dll")' + #13#10 +
                      '  (princ "\nP&ID Standardization plugin loaded successfully! Type PIDINFO for info.\n")' + #13#10 +
                      '  (princ)' + #13#10 +
                      ')';

        // Save the LSP file
        if SaveStringToFile(LspFilePath, LspContent, False) then
        begin
          MsgBox('AutoCAD 2026 has been configured to auto-load the plugin on startup.', mbInformation, MB_OK);
        end
        else
        begin
          MsgBox('Could not create AutoCAD startup file. You may need to configure auto-load manually.' + #13#10 + #13#10 +
                 'See INSTALLATION_GUIDE.txt for instructions.', mbError, MB_OK);
        end;
      end
      else
      begin
        MsgBox('AutoCAD 2026 support directory not found. The plugin will need to be loaded manually using NETLOAD.' + #13#10 + #13#10 +
               'See AutoCAD Plugin Installation Guide for instructions.', mbInformation, MB_OK);
      end;
    end;
  end;
end;

[UninstallDelete]
; Clean up AutoCAD auto-load file on uninstall
Type: files; Name: "{userappdata}\Autodesk\AutoCAD 2026\R24.1\enu\Support\acad2026.lsp"
; Clean up application data
Type: filesandordirs; Name: "{userappdata}\PIDStandardization"
Type: filesandordirs; Name: "{commonappdata}\PIDStandardization"

[Messages]
WelcomeLabel2=This will install [name/ver] on your computer.%n%nThe application provides P&ID equipment tagging and management with AutoCAD integration, supporting both Custom and KKS (DIN 40719) tagging standards.%n%nIt is recommended that you close all other applications before continuing.
