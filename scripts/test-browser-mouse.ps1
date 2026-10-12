. "$PSScriptRoot/toolchain.ps1"
$taskFixtureDirectory = Join-Path ([System.IO.Path]::GetTempPath()) ('PcControllerMouseBrowser-' + [Guid]::NewGuid().ToString('N'))
[System.IO.Directory]::CreateDirectory($taskFixtureDirectory) | Out-Null
$taskFixtureProcess = $null
Push-Location $taskRoot
try {
    & $taskDotnet build tests/PcController.Tests
    Assert-TaskExit
    $taskFixtureProcess = Start-Process -FilePath $taskDotnet -ArgumentList @('tests/PcController.Tests/bin/Debug/net10.0-windows/PcController.Tests.dll', '--serve-mouse-fixture', $taskFixtureDirectory) -WindowStyle Hidden -PassThru
    $taskFixtureMetadata = Join-Path $taskFixtureDirectory 'certificates/fixture.json'
    $taskReadyUntil = [DateTime]::UtcNow.AddSeconds(20)
    while (!(Test-Path -LiteralPath $taskFixtureMetadata)) {
        if ($taskFixtureProcess.HasExited -or [DateTime]::UtcNow -gt $taskReadyUntil) { throw 'Mouse browser fixture did not start. Close the companion using port 8443 first.' }
        Start-Sleep -Milliseconds 100
    }
    $env:PC_CONTROLLER_MOUSE_FIXTURE = $taskFixtureMetadata
    $env:PC_CONTROLLER_BROWSER_TLS_BYPASS = '1'
    Push-Location src/remote
    try { npm.cmd test -- mouse.spec.ts connection.spec.ts; Assert-TaskExit } finally { Pop-Location }
} finally {
    Remove-Item Env:\PC_CONTROLLER_MOUSE_FIXTURE -ErrorAction SilentlyContinue
    Remove-Item Env:\PC_CONTROLLER_BROWSER_TLS_BYPASS -ErrorAction SilentlyContinue
    Set-Content -LiteralPath (Join-Path $taskFixtureDirectory 'stop') -Value 'stop'
    if ($taskFixtureProcess -and !$taskFixtureProcess.WaitForExit(10000)) { $taskFixtureProcess.Kill($true) }
    # Remove only this script's unique temporary directory after the test server releases it.
    $taskResolvedFixture = [System.IO.Path]::GetFullPath($taskFixtureDirectory)
    $taskExpectedParent = [System.IO.Path]::GetFullPath([System.IO.Path]::GetTempPath())
    if ($taskResolvedFixture.StartsWith($taskExpectedParent, [StringComparison]::OrdinalIgnoreCase) -and ([System.IO.Path]::GetFileName($taskResolvedFixture)).StartsWith('PcControllerMouseBrowser-')) {
        Remove-Item -LiteralPath $taskResolvedFixture -Recurse -Force
    }
    Pop-Location
}
