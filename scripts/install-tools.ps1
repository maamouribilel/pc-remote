# Downloads official, pinned portable toolchains without changing global installations.
$ErrorActionPreference = 'Stop'
if ($env:OS -ne 'Windows_NT') { throw 'This installer fetches Windows archives. On macOS/Linux install the pinned native SDK and Node/npm listed in README.md, then use scripts/portable.py.' }
$taskRoot = Split-Path $PSScriptRoot -Parent
$taskTools = Join-Path $taskRoot '.tools'
New-Item -ItemType Directory -Path $taskTools -Force | Out-Null
function Get-VerifiedToolArchive([string]$Url, [string]$Name, [string]$Algorithm, [string]$Expected) {
    $taskArchive = Join-Path $taskTools $Name
    if (-not (Test-Path -LiteralPath $taskArchive) -or (Get-FileHash -LiteralPath $taskArchive -Algorithm $Algorithm).Hash -ne $Expected) {
        Invoke-WebRequest -Uri $Url -OutFile $taskArchive
    }
    if ((Get-FileHash -LiteralPath $taskArchive -Algorithm $Algorithm).Hash -ne $Expected) { throw "Checksum mismatch: $Name" }
    return $taskArchive
}
if (-not (Test-Path -LiteralPath (Join-Path $taskTools 'dotnet/dotnet.exe'))) {
    $taskArchive = Get-VerifiedToolArchive 'https://builds.dotnet.microsoft.com/dotnet/Sdk/10.0.402/dotnet-sdk-10.0.402-win-x64.zip' 'dotnet-sdk-10.0.402-win-x64.zip' 'SHA512' 'ae4fd528cb7e9f6af6aaf7de36715a69f761f183744f1bb3025d19f5a92447090ec561410e46266c1d10a63781ed6f3de771760bc76c876d87daabc367b898cd'
    Expand-Archive -LiteralPath $taskArchive -DestinationPath (Join-Path $taskTools 'dotnet') -Force
}
if (-not (Test-Path -LiteralPath (Join-Path $taskTools 'node-v24.21.0-win-x64/node.exe'))) {
    $taskArchive = Get-VerifiedToolArchive 'https://nodejs.org/dist/v24.21.0/node-v24.21.0-win-x64.zip' 'node-v24.21.0-win-x64.zip' 'SHA256' '158F7685B44DE51F6C0DF1D153526CBCD3E1BC739A8DFC607721CEF75DE9E541'
    Expand-Archive -LiteralPath $taskArchive -DestinationPath $taskTools -Force
}
. "$PSScriptRoot/toolchain.ps1"
& $taskDotnet --version; Assert-TaskExit
node --version; Assert-TaskExit
npm.cmd --version; Assert-TaskExit
