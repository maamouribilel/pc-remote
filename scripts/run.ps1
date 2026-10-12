param([ValidateSet('Debug','Release')][string]$Configuration = 'Debug')
. "$PSScriptRoot/toolchain.ps1"
$taskExecutable = Join-Path $taskRoot "src/PcController.App/bin/$Configuration/net10.0-windows/PcController.App.exe"
if (-not (Test-Path -LiteralPath $taskExecutable)) { throw 'Run scripts/build.ps1 first.' }
Start-Process -FilePath $taskExecutable -WindowStyle Hidden
