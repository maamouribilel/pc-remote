. "$PSScriptRoot/toolchain.ps1"
$taskCompilerDirectory = Join-Path $taskRoot '.tools/InnoSetup-7.1.0'
if (Test-Path -LiteralPath (Join-Path $taskCompilerDirectory 'ISCC.exe')) { return }
New-Item -ItemType Directory -Path (Join-Path $taskRoot '.tools') -Force | Out-Null
$taskArchive = Join-Path $taskRoot '.tools/innosetup-7.1.0-x64.exe'
$taskExpectedHash = '0362a383ed217d4c4239b5933866dd96d3eb2102737da92f80f6057a4b40df2f'
if (!(Test-Path -LiteralPath $taskArchive) -or (Get-FileHash -LiteralPath $taskArchive -Algorithm SHA256).Hash -ne $taskExpectedHash) {
    Invoke-WebRequest 'https://github.com/jrsoftware/issrc/releases/download/is-7_1_0/innosetup-7.1.0-x64.exe' -OutFile $taskArchive
}
$taskSignature = Get-AuthenticodeSignature -LiteralPath $taskArchive
if ((Get-FileHash -LiteralPath $taskArchive -Algorithm SHA256).Hash -ne $taskExpectedHash -or $taskSignature.Status -ne 'Valid' -or $taskSignature.SignerCertificate.Subject -notlike '*Pyrsys B.V.*') { throw 'Official installer compiler verification failed.' }
$taskProcess = Start-Process -FilePath $taskArchive -ArgumentList @('/VERYSILENT','/SUPPRESSMSGBOXES','/NORESTART','/CURRENTUSER','/NOICONS',('/DIR="'+$taskCompilerDirectory+'"')) -WindowStyle Hidden -PassThru -Wait
if ($taskProcess.ExitCode -ne 0) { throw 'Installer compiler installation failed.' }
