$ErrorActionPreference = 'Stop'
$taskRoot = Split-Path $PSScriptRoot -Parent
$taskWindows = $env:OS -eq 'Windows_NT'
$taskDotnetName = if ($taskWindows) { 'dotnet.exe' } else { 'dotnet' }
$taskDotnet = Join-Path $taskRoot ".tools/dotnet/$taskDotnetName"
if (-not (Test-Path -LiteralPath $taskDotnet)) { $taskDotnet = (Get-Command dotnet -ErrorAction Stop).Source }
$env:DOTNET_ROOT = Split-Path $taskDotnet -Parent
$taskArch = if ([Runtime.InteropServices.RuntimeInformation]::OSArchitecture -eq 'Arm64') { 'arm64' } else { 'x64' }
if ($taskArch -eq 'arm64') { $env:DOTNET_ROOT_ARM64 = $env:DOTNET_ROOT } else { $env:DOTNET_ROOT_X64 = $env:DOTNET_ROOT }
$taskOs = if ($taskWindows) { 'win' } elseif ($IsMacOS) { 'darwin' } else { 'linux' }
$taskNodeDirectory = Join-Path $taskRoot ".tools/node-v24.21.0-$taskOs-$taskArch"
if (!$taskWindows) { $taskNodeDirectory = Join-Path $taskNodeDirectory 'bin' }
if (Test-Path -LiteralPath $taskNodeDirectory) { $env:PATH = "$taskNodeDirectory$([IO.Path]::PathSeparator)$env:PATH" }
$taskNpm = if ($taskWindows) { 'npm.cmd' } else { 'npm' }
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
$env:AVALONIA_TELEMETRY_OPTOUT = '1'
$env:DOTNET_NOLOGO = '1'
$env:DOTNET_GENERATE_ASPNET_CERTIFICATE = 'false'
function Assert-TaskExit { if ($LASTEXITCODE -ne 0) { throw "Command failed with exit code $LASTEXITCODE" } }
