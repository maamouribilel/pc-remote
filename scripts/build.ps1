param([ValidateSet('Debug','Release')][string]$Configuration = 'Debug', [switch]$Portable)
. "$PSScriptRoot/toolchain.ps1"
Push-Location $taskRoot
try {
    Push-Location src/remote
    try { & $taskNpm ci; Assert-TaskExit; & $taskNpm run build; Assert-TaskExit } finally { Pop-Location }
    $taskSolution = if ($Portable -or !$taskWindows) { 'PcController.Portable.slnx' } else { 'PcController.slnx' }
    & $taskDotnet build $taskSolution -c $Configuration; Assert-TaskExit
} finally { Pop-Location }
