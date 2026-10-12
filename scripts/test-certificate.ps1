. "$PSScriptRoot/toolchain.ps1"
$taskPython = (Get-Command python -ErrorAction Stop).Source
$taskFixtureDirectory = Join-Path ([IO.Path]::GetTempPath()) ('PcControllerTls-' + [Guid]::NewGuid().ToString('N'))
[IO.Directory]::CreateDirectory($taskFixtureDirectory) | Out-Null
$taskFixtureProcess = $null
Push-Location $taskRoot
try {
    & $taskDotnet build tests/PcController.Tests
    Assert-TaskExit
    $taskFixtureProcess = Start-Process -FilePath $taskDotnet -ArgumentList @('tests/PcController.Tests/bin/Debug/net10.0-windows/PcController.Tests.dll', '--serve-mouse-fixture', $taskFixtureDirectory) -WindowStyle Hidden -PassThru
    $taskMetadata = Join-Path $taskFixtureDirectory 'certificates/fixture.json'
    $taskReadyUntil = [DateTime]::UtcNow.AddSeconds(20)
    while (!(Test-Path -LiteralPath $taskMetadata)) {
        if ($taskFixtureProcess.HasExited -or [DateTime]::UtcNow -gt $taskReadyUntil) { throw 'TLS fixture did not start. Close the companion using port 8443 first.' }
        Start-Sleep -Milliseconds 100
    }
    & $taskPython tests/certificate-trust.py (Join-Path $taskFixtureDirectory 'certificates/pc-remote-root.cer')
    Assert-TaskExit
} finally {
    Set-Content -LiteralPath (Join-Path $taskFixtureDirectory 'stop') -Value 'stop'
    if ($taskFixtureProcess -and !$taskFixtureProcess.WaitForExit(10000)) { $taskFixtureProcess.Kill($true) }
    $taskResolved = [IO.Path]::GetFullPath($taskFixtureDirectory)
    $taskParent = [IO.Path]::GetFullPath([IO.Path]::GetTempPath()).TrimEnd('\') + '\'
    if ($taskResolved.StartsWith($taskParent, [StringComparison]::OrdinalIgnoreCase) -and [IO.Path]::GetFileName($taskResolved).StartsWith('PcControllerTls-')) {
        Remove-Item -LiteralPath $taskResolved -Recurse -Force
    }
    Pop-Location
}
