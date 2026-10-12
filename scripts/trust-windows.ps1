param([ValidateSet('Install','Remove')][string]$Action = 'Install')
$ErrorActionPreference = 'Stop'
$taskRootCertificate = Join-Path $env:LOCALAPPDATA 'PcController/certificates/pc-remote-root.cer'
if (-not (Test-Path -LiteralPath $taskRootCertificate)) { throw 'Run the companion once to create its local certificate.' }
$taskCertificate = [Security.Cryptography.X509Certificates.X509Certificate2]::new($taskRootCertificate)
$taskStore = [Security.Cryptography.X509Certificates.X509Store]::new('Root', 'CurrentUser')
try {
    $taskStore.Open([Security.Cryptography.X509Certificates.OpenFlags]::ReadWrite)
    if ($Action -eq 'Install') { $taskStore.Add($taskCertificate) }
    else {
        foreach ($taskMatch in $taskStore.Certificates.Find([Security.Cryptography.X509Certificates.X509FindType]::FindByThumbprint, $taskCertificate.Thumbprint, $false)) { $taskStore.Remove($taskMatch); $taskMatch.Dispose() }
    }
    $taskStore.Close()
    $taskStore.Open([Security.Cryptography.X509Certificates.OpenFlags]::ReadOnly)
    $taskPersisted = $taskStore.Certificates.Find([Security.Cryptography.X509Certificates.X509FindType]::FindByThumbprint, $taskCertificate.Thumbprint, $false).Count -gt 0
    if ($Action -eq 'Install' -and -not $taskPersisted) { throw 'Windows did not retain this CurrentUser root entry. Check certificate-store policy or install/review it manually; do not bypass warnings for actual onboarding.' }
    if ($Action -eq 'Remove' -and $taskPersisted) { throw 'Windows still has the root entry. Remove this installation certificate manually in certmgr.msc.' }
    Write-Output "$Action completed for this installation's public root in CurrentUser only."
} finally { $taskStore.Dispose(); $taskCertificate.Dispose() }
