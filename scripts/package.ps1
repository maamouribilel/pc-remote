param([string]$Version = '0.3.5')
. "$PSScriptRoot/toolchain.ps1"
if ($Version -notmatch '^\d+\.\d+\.\d+$') { throw 'Use a three-component release version.' }
& "$PSScriptRoot/check-release-docs.ps1" -Version $Version
& "$PSScriptRoot/install-packaging-tool.ps1"
& "$PSScriptRoot/build.ps1" -Configuration Release
$taskReleaseDirectory = Join-Path $taskRoot ('artifacts/release-' + $Version + '-' + [DateTime]::UtcNow.ToString('yyyyMMdd-HHmmss'))
$taskPublishDirectory = Join-Path $taskReleaseDirectory 'PCRemote-win-x64'
[IO.Directory]::CreateDirectory($taskPublishDirectory) | Out-Null
# Public downloads omit developer debug symbols and their local build paths.
& $taskDotnet publish (Join-Path $taskRoot 'src/PcController.App') -c Release -r win-x64 --self-contained true -p:PublishTrimmed=false -p:PublishSingleFile=false -p:DebugType=None -p:DebugSymbols=false -p:Version=$Version -o $taskPublishDirectory
Assert-TaskExit
Copy-Item -LiteralPath (Join-Path $taskRoot 'LICENSE') -Destination (Join-Path $taskPublishDirectory 'LICENSE')
function Copy-ReleaseGuide([string]$Source, [string]$Name) {
    $taskSourcePath = Join-Path $taskRoot $Source
    $taskGuide = [IO.File]::ReadAllText($taskSourcePath)
    # Guides are flattened/renamed in the distribution. Point source-relative
    # Markdown links to the exact release tag instead of shipping broken paths.
    $taskGuide = [regex]::Replace($taskGuide, '(\[[^\]]*\]\()([^\s)]+)(\))', {
        param($taskMatch)
        $taskTarget = $taskMatch.Groups[2].Value
        if ($taskTarget -match '^[a-zA-Z][a-zA-Z0-9+.-]*:|^#') { return $taskMatch.Value }
        $taskParts = $taskTarget -split '#', 2
        $taskTargetPath = [IO.Path]::GetFullPath((Join-Path (Split-Path $taskSourcePath -Parent) $taskParts[0]))
        $taskRootPrefix = [IO.Path]::GetFullPath($taskRoot).TrimEnd('\') + '\'
        if (!$taskTargetPath.StartsWith($taskRootPrefix, [StringComparison]::OrdinalIgnoreCase)) { throw 'Release guide link leaves the source repository.' }
        $taskRelative = $taskTargetPath.Substring($taskRootPrefix.Length).Replace('\', '/')
        $taskAnchor = if ($taskParts.Length -eq 2) { '#' + $taskParts[1] } else { '' }
        return $taskMatch.Groups[1].Value + "https://github.com/maamouribilel/windows-pc-mobile-controller/blob/v$Version/$taskRelative$taskAnchor" + $taskMatch.Groups[3].Value
    })
    [IO.File]::WriteAllText((Join-Path $taskPublishDirectory $Name), $taskGuide)
}
Copy-ReleaseGuide 'docs/setup.md' 'SETUP.md'
Copy-ReleaseGuide 'docs/pc-features.md' 'PC-CONTROLS.md'
Copy-ReleaseGuide 'docs/limitations.md' 'LIMITATIONS.md'
Copy-ReleaseGuide 'docs/certificates.md' 'CERTIFICATES.md'
Copy-ReleaseGuide 'docs/third-party.md' 'THIRD-PARTY.md'
Copy-ReleaseGuide 'docs/troubleshooting.md' 'TROUBLESHOOTING.md'
Copy-ReleaseGuide 'docs/release-checklist.md' 'MANUAL-TESTS.md'
$taskLicenses = Join-Path $taskPublishDirectory 'licenses'
[IO.Directory]::CreateDirectory($taskLicenses) | Out-Null
Copy-Item -LiteralPath (Join-Path (Split-Path $taskDotnet -Parent) 'LICENSE.txt') -Destination (Join-Path $taskLicenses 'DOTNET-LICENSE.txt')
Copy-Item -LiteralPath (Join-Path (Split-Path $taskDotnet -Parent) 'ThirdPartyNotices.txt') -Destination (Join-Path $taskLicenses 'DOTNET-ThirdPartyNotices.txt')
$taskNugetRoot = if ($env:NUGET_PACKAGES) { $env:NUGET_PACKAGES } else { Join-Path $env:USERPROFILE '.nuget/packages' }
Copy-Item -LiteralPath (Join-Path $taskNugetRoot 'qrcoder/1.8.0/LICENSE.txt') -Destination (Join-Path $taskLicenses 'QRCODER-LICENSE.txt')
$taskCompiler = Join-Path $taskRoot '.tools/InnoSetup-7.1.0/ISCC.exe'
& $taskCompiler --quiet ("--define=PublishDir=" + $taskPublishDirectory) ("--define=ReleaseDir=" + $taskReleaseDirectory) ("--define=AppVersion=" + $Version) (Join-Path $taskRoot 'installer/PCRemote.iss')
Assert-TaskExit
$taskZip = Join-Path $taskReleaseDirectory ('PCRemote-' + $Version + '-win-x64.zip')
Compress-Archive -LiteralPath $taskPublishDirectory -DestinationPath $taskZip
$taskInstaller = Join-Path $taskReleaseDirectory ('PCRemote-' + $Version + '-win-x64-setup.exe')
@($taskInstaller,$taskZip) | ForEach-Object { $taskHash = Get-FileHash -LiteralPath $_ -Algorithm SHA256; $taskHash.Hash.ToLowerInvariant() + '  ' + [IO.Path]::GetFileName($_) } | Set-Content -LiteralPath (Join-Path $taskReleaseDirectory 'SHA256SUMS.txt')
[IO.File]::WriteAllText((Join-Path $taskRoot 'artifacts/latest-release.txt'),$taskReleaseDirectory)
Write-Output "Release: $taskReleaseDirectory"
