param([string]$Executable)
. "$PSScriptRoot/toolchain.ps1"
$taskExecutable = if ($Executable) { [IO.Path]::GetFullPath($Executable) } else { Join-Path $taskRoot 'src/PcController.App/bin/Release/net10.0-windows/PcController.App.exe' }
& $taskDotnet run --project (Join-Path $taskRoot 'tests/PcController.Tests') -- --desktop $taskExecutable
Assert-TaskExit
