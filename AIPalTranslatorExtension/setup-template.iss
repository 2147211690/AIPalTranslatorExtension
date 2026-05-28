#define AppVersion "0.0.1.0"

[Setup]
AppId={{C3C96A87-E1E8-55A9-1388-9F618A721589}}
AppName=AI Pal Translator Extension
AppVersion={#AppVersion}
AppPublisher=Noper
DefaultDirName={autopf}\AIPalTranslatorExtension
OutputDir=bin\Release\installer
OutputBaseFilename=AIPalTranslatorExtension-Setup-{#AppVersion}
Compression=lzma
SolidCompression=yes
MinVersion=10.0.19041

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"
Name: "chinesesimplified"; MessagesFile: "compiler:Languages\ChineseSimplified.isl"

[Files]
Source: "bin\Release\win-x64\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs

[Icons]
Name: "{group}\AI Pal Translator Extension"; Filename: "{app}\AIPalTranslatorExtension.exe"

[Registry]
Root: HKCU; Subkey: "SOFTWARE\Classes\CLSID\{{832677a3-a07d-413e-82b7-6dca9b29e9ff}}"; ValueData: "AIPalTranslatorExtension"
Root: HKCU; Subkey: "SOFTWARE\Classes\CLSID\{{832677a3-a07d-413e-82b7-6dca9b29e9ff}}\LocalServer32"; ValueData: "{app}\AIPalTranslatorExtension.exe -RegisterProcessAsComServer"