param([ValidateSet('Debug','Release')][string]$Configuration = 'Debug')
. "$PSScriptRoot/toolchain.ps1"
Push-Location $taskRoot
try {
    Push-Location src/remote
    try { npm.cmd ci; Assert-TaskExit; npm.cmd run build; Assert-TaskExit } finally { Pop-Location }
    & $taskDotnet build PcController.slnx -c $Configuration; Assert-TaskExit
} finally { Pop-Location }
