param([string]$Version = '0.2.0')
. "$PSScriptRoot/toolchain.ps1"
if ($Version -notmatch '^\d+\.\d+\.\d+$') { throw 'Use a three-component release version.' }
& "$PSScriptRoot/install-packaging-tool.ps1"
& "$PSScriptRoot/build.ps1" -Configuration Release
$taskReleaseDirectory = Join-Path $taskRoot ('artifacts/release-' + $Version + '-' + [DateTime]::UtcNow.ToString('yyyyMMdd-HHmmss'))
$taskPublishDirectory = Join-Path $taskReleaseDirectory 'PCRemote-win-x64'
[IO.Directory]::CreateDirectory($taskPublishDirectory) | Out-Null
# Public downloads omit developer debug symbols and their local build paths.
& $taskDotnet publish (Join-Path $taskRoot 'src/PcController.App') -c Release -r win-x64 --self-contained true -p:PublishTrimmed=false -p:PublishSingleFile=false -p:DebugType=None -p:DebugSymbols=false -p:Version=$Version -o $taskPublishDirectory
Assert-TaskExit
Copy-Item -LiteralPath (Join-Path $taskRoot 'LICENSE') -Destination (Join-Path $taskPublishDirectory 'LICENSE')
Copy-Item -LiteralPath (Join-Path $taskRoot 'docs/setup.md') -Destination (Join-Path $taskPublishDirectory 'SETUP.md')
Copy-Item -LiteralPath (Join-Path $taskRoot 'docs/pc-features.md') -Destination (Join-Path $taskPublishDirectory 'PC-CONTROLS.md')
Copy-Item -LiteralPath (Join-Path $taskRoot 'docs/limitations.md') -Destination (Join-Path $taskPublishDirectory 'LIMITATIONS.md')
Copy-Item -LiteralPath (Join-Path $taskRoot 'docs/certificates.md') -Destination (Join-Path $taskPublishDirectory 'CERTIFICATES.md')
Copy-Item -LiteralPath (Join-Path $taskRoot 'docs/third-party.md') -Destination (Join-Path $taskPublishDirectory 'THIRD-PARTY.md')
Copy-Item -LiteralPath (Join-Path $taskRoot 'docs/troubleshooting.md') -Destination (Join-Path $taskPublishDirectory 'TROUBLESHOOTING.md')
Copy-Item -LiteralPath (Join-Path $taskRoot 'docs/release-checklist.md') -Destination (Join-Path $taskPublishDirectory 'MANUAL-TESTS.md')
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
