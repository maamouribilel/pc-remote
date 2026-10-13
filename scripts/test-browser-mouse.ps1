. "$PSScriptRoot/toolchain.ps1"
$taskPython = if (Get-Command python3 -ErrorAction SilentlyContinue) { 'python3' } else { 'python' }
& $taskPython (Join-Path $PSScriptRoot 'portable.py') browser
Assert-TaskExit
