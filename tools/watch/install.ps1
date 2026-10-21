# Registers the daily watch (tools/watch/daily.ps1, doc 03 §5b) in Windows Task Scheduler for this user.
#   powershell -File tools/watch/install.ps1              every day at 04:00 (a missed run starts when the PC is next on)
#   powershell -File tools/watch/install.ps1 -At 13:30    another time
#   powershell -File tools/watch/install.ps1 -Uninstall   remove it
# It runs only while you are logged on: the game test needs Steam and the desktop.
param([string]$At = '04:00', [switch]$Uninstall)

$Name = 'DeadlockOfRain daily watch'
if ($Uninstall) {
    Unregister-ScheduledTask -TaskName $Name -Confirm:$false
    Write-Host "removed '$Name'"
    exit 0
}
$script = Join-Path $PSScriptRoot 'daily.ps1'
$action = New-ScheduledTaskAction -Execute 'powershell.exe' `
    -Argument "-NoProfile -ExecutionPolicy Bypass -WindowStyle Hidden -File `"$script`"" `
    -WorkingDirectory (Split-Path (Split-Path $PSScriptRoot))
$trigger = New-ScheduledTaskTrigger -Daily -At $At
$settings = New-ScheduledTaskSettingsSet -StartWhenAvailable -MultipleInstances IgnoreNew `
    -ExecutionTimeLimit (New-TimeSpan -Hours 6) -AllowStartIfOnBatteries -DontStopIfGoingOnBatteries
$principal = New-ScheduledTaskPrincipal -UserId "$env:USERDOMAIN\$env:USERNAME" -LogonType Interactive -RunLevel Limited
Register-ScheduledTask -TaskName $Name -Action $action -Trigger $trigger -Settings $settings -Principal $principal -Force | Out-Null
Write-Host "registered '$Name': daily at $At, runs $script"
