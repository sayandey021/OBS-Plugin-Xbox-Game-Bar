Write-Host "=== Restarting AppX deployment services ==="
Restart-Service -Name AppXSvc -Force -ErrorAction Continue
Start-Sleep -Seconds 5
Get-Service AppXSvc | Select-Object Name, Status

Write-Host "=== Killing zombie widget process (elevated) ==="
Get-Process -Name 'OBSGameBar' -ErrorAction SilentlyContinue | ForEach-Object {
    Write-Host "Killing $($_.Id)"
    Stop-Process -Id $_.Id -Force -ErrorAction Continue
}
Start-Sleep -Seconds 3

Write-Host "=== Removing package (elevated) ==="
$pkg = Get-AppxPackage -Name 'OBSGameBarWidget' -ErrorAction SilentlyContinue
if ($pkg) { $pkg | Remove-AppxPackage -ErrorAction Continue }
Start-Sleep -Seconds 8
Get-AppxPackage -Name 'OBSGameBarWidget' | Select-Object Status
Write-Host "=== ELEVATED FIX COMPLETE - this window can be closed ==="
