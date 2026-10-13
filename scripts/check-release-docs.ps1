param([string]$Version, [string]$BaseRef)
$ErrorActionPreference = 'Stop'
$taskRepository = Split-Path $PSScriptRoot -Parent

function Read-ReleaseFile([string]$Path) {
    $taskPath = Join-Path $taskRepository $Path
    if (-not (Test-Path -LiteralPath $taskPath -PathType Leaf)) { throw "Missing release file: $Path" }
    return [IO.File]::ReadAllText($taskPath)
}

$taskAppPath = 'src/PcController.App/PcController.App.csproj'
$taskProject = [xml](Read-ReleaseFile $taskAppPath)
$taskAppVersion = [string]$taskProject.Project.PropertyGroup.Version
if ($taskAppVersion -notmatch '^\d+\.\d+\.\d+$') { throw 'The app must declare a three-component Version.' }
if ($Version -and $Version -cne $taskAppVersion) { throw "Requested version $Version differs from app version $taskAppVersion. Update release files first." }
$Version = $taskAppVersion
$taskVersionPattern = [regex]::Escape($Version)
$taskSharedProject = [xml](Read-ReleaseFile 'Directory.Build.props')
if ([string]$taskSharedProject.Project.PropertyGroup.PcRemoteVersion -cne $Version) { throw 'Shared PcRemoteVersion must match the Windows app.' }
$taskDesktopProject = [xml](Read-ReleaseFile 'src/PcController.Desktop/PcController.Desktop.csproj')
if ([string]$taskDesktopProject.Project.PropertyGroup.Version -cne '$(PcRemoteVersion)') { throw 'Desktop must inherit the shared product version.' }


$taskPackage = Read-ReleaseFile 'scripts/package.ps1'
if ($taskPackage -notmatch ('param\(\[string\]\$Version = ''' + $taskVersionPattern + '''\)')) {
    throw "scripts/package.ps1 default must be $Version."
}
$taskInstaller = Read-ReleaseFile 'installer/PCRemote.iss'
if ($taskInstaller -notmatch ('#define AppVersion "' + $taskVersionPattern + '"')) { throw "Installer fallback must be $Version." }

$taskReleaseUrl = 'https://github.com/maamouribilel/windows-pc-mobile-controller/releases'
$taskTagUrl = "$taskReleaseUrl/tag/v$Version"
$taskAssets = @("PCRemote-$Version-win-x64-setup.exe", "PCRemote-$Version-win-x64.zip", 'SHA256SUMS.txt')
foreach ($taskDoc in @('README.md', 'docs/releases.md')) {
    $taskContent = Read-ReleaseFile $taskDoc
    if (-not $taskContent.Contains($taskTagUrl)) { throw "$taskDoc needs the current release notes link: $taskTagUrl" }
    foreach ($taskAsset in $taskAssets) {
        $taskUrl = "$taskReleaseUrl/download/v$Version/$taskAsset"
        if (-not $taskContent.Contains($taskUrl)) { throw "$taskDoc needs the download link: $taskUrl" }
    }
    foreach ($taskLink in [regex]::Matches($taskContent, 'https://github\.com/maamouribilel/windows-pc-mobile-controller/releases/(?:tag|download)/v(\d+\.\d+\.\d+)')) {
        if ($taskLink.Groups[1].Value -cne $Version) { throw "$taskDoc has a stale current release link: $($taskLink.Value)" }
    }
}

$taskReadme = Read-ReleaseFile 'README.md'
if (-not $taskReadme.Contains("Download-v$Version")) { throw 'README download badge must show the current app version.' }
if (-not $taskReadme.Contains("./scripts/package.ps1 -Version $Version")) { throw 'README packaging command must use the current app version.' }
foreach ($taskName in [regex]::Matches($taskReadme, 'PCRemote-(\d+\.\d+\.\d+)-win-x64')) {
    if ($taskName.Groups[1].Value -cne $Version) { throw "README has a stale package filename: $($taskName.Value)" }
}

foreach ($taskDoc in @('AGENTS.md', 'CLAUDE.md')) {
    $taskContent = Read-ReleaseFile $taskDoc
    if ($taskContent -notmatch ('Current app version: `' + $taskVersionPattern + '`\.')) { throw "$taskDoc must declare the current app version $Version." }
    if (-not $taskContent.Contains($taskTagUrl)) { throw "$taskDoc needs the current public release link." }
}
$taskEvidence = Read-ReleaseFile 'docs/release-evidence.md'
if ($taskEvidence -notmatch ('(?m)^## Version ' + $taskVersionPattern + '(?:\s|$)')) { throw "Add a Version $Version entry to docs/release-evidence.md with actual results and remaining gaps." }
foreach ($taskRequired in @('docs/release-checklist.md', '.github/pull_request_template.md', '.github/workflows/release-docs.yml')) {
    $null = Read-ReleaseFile $taskRequired
}

if ($BaseRef) {
    Push-Location $taskRepository
    try {
        $taskBaseCommit = & git rev-parse --verify --end-of-options "$BaseRef^{commit}"
        if ($LASTEXITCODE -ne 0) { throw 'Cannot resolve the release comparison base.' }
        $taskPreviousXml = & git show "${taskBaseCommit}:$taskAppPath"
        if ($LASTEXITCODE -ne 0) { throw 'Cannot read the previous app version.' }
        $taskPreviousProject = [xml]($taskPreviousXml -join "`n")
        $taskPreviousVersion = [string]$taskPreviousProject.Project.PropertyGroup.Version
        if ($taskPreviousVersion -cne $Version) {
            $taskChanged = & git diff --name-only "$taskBaseCommit...HEAD"
            if ($LASTEXITCODE -ne 0) { throw 'Cannot compare release documentation changes.' }
            foreach ($taskDoc in @('README.md', 'AGENTS.md', 'CLAUDE.md', 'docs/releases.md', 'docs/release-evidence.md')) {
                if ($taskChanged -notcontains $taskDoc) { throw "Version changed to $Version but $taskDoc was not updated in this release." }
            }
        }
    } finally { Pop-Location }
}

Write-Output "Release documentation is synchronized for v$Version. Runtime checks, secret scans, and public asset verification are still required before publishing."
