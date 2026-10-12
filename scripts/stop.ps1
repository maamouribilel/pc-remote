param([ValidateSet('Debug','Release')][string]$Configuration = 'Debug')
. "$PSScriptRoot/toolchain.ps1"
$taskExecutable = Join-Path $taskRoot "src/PcController.App/bin/$Configuration/net10.0-windows/PcController.App.exe"
$taskRunning = @(Get-Process PcController.App -ErrorAction SilentlyContinue | Where-Object Path -EQ $taskExecutable)
$taskStopProcess = Start-Process -FilePath $taskExecutable -ArgumentList '--exit' -WindowStyle Hidden -PassThru -Wait
if ($taskStopProcess.ExitCode -ne 0) { throw 'Could not reach the same-user companion. Use Exit in the tray menu.' }
foreach ($taskProcess in $taskRunning) { if (-not $taskProcess.WaitForExit(15000)) { throw 'Companion is still shutting down. Wait before rebuilding.' } }
