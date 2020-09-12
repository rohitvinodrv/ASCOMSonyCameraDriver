;
; Script generated by the ASCOM Driver Installer Script Generator 6.4.0.0
; Generated by Doug Henderson on 12/19/2019 (UTC)
;
[Setup]
AppID={{aa3f61ab-dde5-41ac-ad0e-e00604609f71}
AppName=ASCOM Sony Mirrorless Driver Camera Driver
AppVerName=ASCOM Sony Mirrorless Driver Camera Driver 0.0.18
AppVersion=0.0.18
AppPublisher=Doug Henderson <retrodotkiwi@gmail.com>
AppPublisherURL=mailto:retrodotkiwi@gmail.com
AppSupportURL=http://tech.groups.yahoo.com/group/ASCOM-Talk/
AppUpdatesURL=http://ascom-standards.org/
ArchitecturesInstallIn64BitMode=x64
VersionInfoVersion=1.0.1
MinVersion=0,6.0.2195sp4
DefaultDirName="{commoncf}\ASCOM\Camera"
DisableDirPage=yes
DisableProgramGroupPage=yes
OutputDir="."
OutputBaseFilename="Sony Mirrorless Driver Setup"
Compression=lzma
SolidCompression=yes
; Put there by Platform if Driver Installer Support selected
WizardImageFile="C:\Program Files (x86)\ASCOM\Platform 6 Developer Components\Installer Generator\Resources\WizardImage.bmp"
LicenseFile="C:\Users\dougf\source\repos\ASCOMSonyCameraDriver\ASCOMSonyCameraDriver\License Info.txt"
; {commoncf}\ASCOM\Uninstall\Camera folder created by Platform, always
UninstallFilesDir="{commoncf}\ASCOM\Uninstall\Camera\Sony Mirrorless Driver"

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Dirs]
Name: "{commoncf}\ASCOM\Uninstall\Camera\Sony Mirrorless Driver"
; TODO: Add subfolders below {app} as needed (e.g. Name: "{app}\MyFolder")

[Files]
; Require a read-me HTML to appear after installation, maybe driver's Help doc
Source: "C:\Users\dougf\source\repos\ASCOMSonyCameraDriver\ASCOMSonyCameraDriver\Sony Mirrorless Camera ReadMe.htm"; DestDir: "{app}"; Flags: isreadme

; TODO: Add other files needed by your driver here (add subfolders above)
; 32-bit files
Source: "C:\Users\dougf\source\repos\SonyCamera\Release\libraw.dll"; DestDir: "{sys}"; Flags: 32bit
Source: "C:\Users\dougf\source\repos\SonyCamera\Release\SonyMTPCamera.dll"; DestDir: "{sys}"; Flags: 32bit
Source: "C:\Users\dougf\source\repos\SonyCamera\Release\turbojpeg.dll"; DestDir: "{sys}"; Flags: 32bit
Source: "C:\Users\dougf\source\repos\SonyCamera\Release\SonyCameraInfo.exe"; DestDir: "{app}"

; 64-bit files
Source: "C:\Users\dougf\source\repos\SonyCamera\x64\Release\libraw.dll"; DestDir: "{sys}"; Flags: 64bit
Source: "C:\Users\dougf\source\repos\SonyCamera\x64\Release\SonyMTPCamera.dll"; DestDir: "{sys}"; Flags: 64bit
Source: "C:\Users\dougf\source\repos\SonyCamera\x64\Release\turbojpeg.dll"; DestDir: "{sys}"; Flags: 64bit

; AnyCPU files
Source: "C:\Users\dougf\source\repos\ASCOMSonyCameraDriver\ASCOMSonyCameraDriver\bin\Release\ASCOM.SonyMirrorless.Camera.dll"; DestDir: "{app}"

; Only if driver is .NET
[Run]
; Only for .NET assembly/in-proc drivers
Filename: "{dotnet4032}\regasm.exe"; Parameters: "/codebase ""{app}\ASCOM.SonyMirrorless.Camera.dll"""; Flags: runhidden 32bit
Filename: "{dotnet4064}\regasm.exe"; Parameters: "/codebase ""{app}\ASCOM.SonyMirrorless.Camera.dll"""; Flags: runhidden 64bit; Check: IsWin64




; Only if driver is .NET
[UninstallRun]
; Only for .NET assembly/in-proc drivers
Filename: "{dotnet4032}\regasm.exe"; Parameters: "-u ""{app}\ASCOM.SonyMirrorless.Camera.dll"""; Flags: runhidden 32bit
; This helps to give a clean uninstall
Filename: "{dotnet4064}\regasm.exe"; Parameters: "/codebase ""{app}\ASCOM.SonyMirrorless.Camera.dll"""; Flags: runhidden 64bit; Check: IsWin64
Filename: "{dotnet4064}\regasm.exe"; Parameters: "-u ""{app}\ASCOM.SonyMirrorless.Camera.dll"""; Flags: runhidden 64bit; Check: IsWin64




[Code]
const
   REQUIRED_PLATFORM_VERSION = 6.2;    // Set this to the minimum required ASCOM Platform version for this application

//
// Function to return the ASCOM Platform's version number as a double.
//
function PlatformVersion(): Double;
var
   PlatVerString : String;
begin
   Result := 0.0;  // Initialise the return value in case we can't read the registry
   try
      if RegQueryStringValue(HKEY_LOCAL_MACHINE_32, 'Software\ASCOM','PlatformVersion', PlatVerString) then 
      begin // Successfully read the value from the registry
         Result := StrToFloat(PlatVerString); // Create a double from the X.Y Platform version string
      end;
   except                                                                   
      ShowExceptionMessage;
      Result:= -1.0; // Indicate in the return value that an exception was generated
   end;
end;

//
// Before the installer UI appears, verify that the required ASCOM Platform version is installed.
//
function InitializeSetup(): Boolean;
var
   PlatformVersionNumber : double;
 begin
   Result := FALSE;  // Assume failure
   PlatformVersionNumber := PlatformVersion(); // Get the installed Platform version as a double
   If PlatformVersionNumber >= REQUIRED_PLATFORM_VERSION then	// Check whether we have the minimum required Platform or newer
      Result := TRUE
   else
      if PlatformVersionNumber = 0.0 then
         MsgBox('No ASCOM Platform is installed. Please install Platform ' + Format('%3.1f', [REQUIRED_PLATFORM_VERSION]) + ' or later from http://www.ascom-standards.org', mbCriticalError, MB_OK)
      else 
         MsgBox('ASCOM Platform ' + Format('%3.1f', [REQUIRED_PLATFORM_VERSION]) + ' or later is required, but Platform '+ Format('%3.1f', [PlatformVersionNumber]) + ' is installed. Please install the latest Platform before continuing; you will find it at http://www.ascom-standards.org', mbCriticalError, MB_OK);
end;

// Code to enable the installer to uninstall previous versions of itself when a new version is installed
procedure CurStepChanged(CurStep: TSetupStep);
var
  ResultCode: Integer;
  UninstallExe: String;
  UninstallRegistry: String;
begin
  if (CurStep = ssInstall) then // Install step has started
	begin
      // Create the correct registry location name, which is based on the AppId
      UninstallRegistry := ExpandConstant('Software\Microsoft\Windows\CurrentVersion\Uninstall\{#SetupSetting("AppId")}' + '_is1');
      // Check whether an extry exists
      if RegQueryStringValue(HKLM, UninstallRegistry, 'UninstallString', UninstallExe) then
        begin // Entry exists and previous version is installed so run its uninstaller quietly after informing the user
          MsgBox('Setup will now remove the previous version.', mbInformation, MB_OK);
          Exec(RemoveQuotes(UninstallExe), ' /SILENT', '', SW_SHOWNORMAL, ewWaitUntilTerminated, ResultCode);
          sleep(1000);    //Give enough time for the install screen to be repainted before continuing
        end
  end;
end;

