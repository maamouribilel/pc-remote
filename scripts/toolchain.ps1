$ErrorActionPreference = 'Stop'
$taskRoot = Split-Path $PSScriptRoot -Parent
$taskDotnet = Join-Path $taskRoot '.tools/dotnet/dotnet.exe'
if (-not (Test-Path -LiteralPath $taskDotnet)) { $taskDotnet = (Get-Command dotnet -ErrorAction Stop).Source }
$env:DOTNET_ROOT = Split-Path $taskDotnet -Parent
$env:DOTNET_ROOT_X64 = $env:DOTNET_ROOT
$taskNodeDirectory = Join-Path $taskRoot '.tools/node-v24.21.0-win-x64'
if (Test-Path -LiteralPath $taskNodeDirectory) { $env:PATH = "$taskNodeDirectory;$env:PATH" }
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
$env:DOTNET_NOLOGO = '1'
function Assert-TaskExit { if ($LASTEXITCODE -ne 0) { throw "Command failed with exit code $LASTEXITCODE" } }
