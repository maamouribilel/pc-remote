param([switch]$Integration, [switch]$Browser, [switch]$Lan, [switch]$Pairing, [switch]$MouseSmoke, [switch]$KeyboardSmoke, [switch]$Lifecycle, [switch]$PcFeatures)
. "$PSScriptRoot/toolchain.ps1"
Push-Location $taskRoot
try {
    if ($PcFeatures) { & $taskDotnet run --project tests/PcController.Tests -- --pc-features }
    elseif ($Lifecycle) { & $taskDotnet run --project tests/PcController.Tests -- --lifecycle }
    elseif ($KeyboardSmoke) { & $taskDotnet run --project tests/PcController.Tests -- --keyboard-smoke }
    elseif ($MouseSmoke) { & $taskDotnet run --project tests/PcController.Tests -- --mouse-smoke }
    elseif ($Pairing) { & $taskDotnet run --project tests/PcController.Tests -- --pairing-integration }
    elseif ($Lan) { & $taskDotnet run --project tests/PcController.Tests -- --integration --lan }
    elseif ($Integration) { & $taskDotnet run --project tests/PcController.Tests -- --integration }
    else { & $taskDotnet run --project tests/PcController.Tests }
    Assert-TaskExit
    if ($Browser) {
        # Browser tests validate certificates normally. Install only this PC's root using
        # scripts/trust-windows.ps1 before running, and remove it afterwards if temporary.
        Push-Location src/remote
        try { npm.cmd test; Assert-TaskExit } finally { Pop-Location }
    }
} finally { Pop-Location }
