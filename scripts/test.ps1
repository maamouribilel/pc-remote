param([switch]$Integration, [switch]$Browser, [switch]$Lan, [switch]$Pairing, [switch]$MouseSmoke, [switch]$KeyboardSmoke, [switch]$Lifecycle, [switch]$PcFeatures, [switch]$Security, [switch]$Portable, [switch]$Desktop)
. "$PSScriptRoot/toolchain.ps1"
Push-Location $taskRoot
try {
    if ($Desktop) { & $taskDotnet run --project tests/PcController.DesktopTests }
    elseif ($Portable -or !$taskWindows) {
        if ($MouseSmoke -or $KeyboardSmoke -or $Integration -or $Lan) { throw 'Native/installed checks require a Windows runner. Use portable fixture checks on this platform.' }
        & $taskDotnet run --project tests/PcController.PortableTests
    }
    elseif ($Security) { & $taskDotnet run --project tests/PcController.Tests -- --security }
    elseif ($PcFeatures) { & $taskDotnet run --project tests/PcController.Tests -- --pc-features }
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
        try { & $taskNpm test; Assert-TaskExit } finally { Pop-Location }
    }
} finally { Pop-Location }
