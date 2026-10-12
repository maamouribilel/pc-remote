param([ValidateSet('Install','Remove')][string]$Action = 'Install')
$ErrorActionPreference = 'Stop'
$taskPrincipal = [Security.Principal.WindowsPrincipal]::new([Security.Principal.WindowsIdentity]::GetCurrent())
if (-not $taskPrincipal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) { throw 'Run this script in an administrator PowerShell. Keep the companion itself unelevated.' }
$taskRuleName = 'PcController-Private-TLS-8443'
if ($Action -eq 'Remove') {
    Get-NetFirewallRule -Name $taskRuleName -ErrorAction SilentlyContinue | Remove-NetFirewallRule
} else {
    if (-not (Get-NetConnectionProfile | Where-Object NetworkCategory -eq 'Private')) { throw 'No Private network. Set your trusted home network to Private in Windows Settings first.' }
    $taskExisting = Get-NetFirewallRule -Name $taskRuleName -ErrorAction SilentlyContinue
    if ($taskExisting) { $taskExisting | Remove-NetFirewallRule }
    New-NetFirewallRule -Name $taskRuleName -DisplayName 'PC Remote HTTPS (Private LAN only)' -Direction Inbound -Action Allow -Protocol TCP -LocalPort 8443 -Profile Private -RemoteAddress LocalSubnet | Out-Null
}
