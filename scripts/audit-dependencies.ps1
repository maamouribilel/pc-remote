. "$PSScriptRoot/toolchain.ps1"
Push-Location $taskRoot
try {
    Push-Location src/remote
    try { npm.cmd audit --audit-level=low; Assert-TaskExit } finally { Pop-Location }
    # dotnet package listing exits successfully even when it finds vulnerabilities.
    # Inspect its structured result so CI fails on actual advisory findings too.
    $taskAuditJson = & $taskDotnet list PcController.slnx package --vulnerable --include-transitive --format json
    Assert-TaskExit
    $taskAudit = ($taskAuditJson -join "`n") | ConvertFrom-Json
    if (!$taskAudit.projects -or $taskAudit.errors) { throw 'NuGet vulnerability audit did not return a usable project result.' }
    foreach ($taskProject in $taskAudit.projects) {
        foreach ($taskFramework in $taskProject.frameworks) {
            foreach ($taskPackage in @($taskFramework.topLevelPackages) + @($taskFramework.transitivePackages)) {
                if ($taskPackage.vulnerabilities) { throw "NuGet advisory found for $($taskPackage.id). Review dotnet package audit output before releasing." }
            }
        }
    }
    Write-Output 'npm and NuGet audits found no dependency vulnerabilities in the current feeds.'
} finally { Pop-Location }
