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
    $taskArchive = Get-VerifiedToolArchive 'https://builds.dotnet.microsoft.com/dotnet/Sdk/10.0.401/dotnet-sdk-10.0.401-win-x64.zip' 'dotnet-sdk.zip' 'SHA512' '24b670ad3d923bfcf47df6c3b034152398b42f6dbc388e10d783aee1cfb5e5817d399fc0ae2a12cfa822a55e61d34830ccb15c50ef6efee437ab874bb7c79430'
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
