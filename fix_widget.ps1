# Run this from an ELEVATED (Administrator) PowerShell to fix the stuck
# OBS Game Bar widget deployment. After running it, rebuild in VS and press F5.

Write-Host "=== 1. Killing Game Bar and widget processes ==="
foreach ($name in 'OBSGameBar', 'GameBar', 'GameBarFTServer', 'GameBarPresenceWriter') {
    Get-Process -Name $name -ErrorAction SilentlyContinue | ForEach-Object {
        Write-Host "Stopping $($_.Name) (PID $($_.Id))"
        Stop-Process -Id $_.Id -Force -ErrorAction SilentlyContinue
    }
}
Start-Sleep -Seconds 3

Write-Host "=== 2. Removing stuck package ==="
$pkg = Get-AppxPackage -Name 'OBSGameBarWidget' -ErrorAction SilentlyContinue
if ($pkg) {
    Write-Host "Removing $($pkg.PackageFullName) (status: $($pkg.Status))"
    $pkg | Remove-AppxPackage -ErrorAction Continue
    Start-Sleep -Seconds 5
} else {
    Write-Host "Package not registered - nothing to remove."
}

$pkg2 = Get-AppxPackage -Name 'OBSGameBarWidget' -ErrorAction SilentlyContinue
if ($pkg2) {
    Write-Host "WARNING: package still present with status $($pkg2.Status)."
    Write-Host "A REBOOT is required to clear the stuck deployment, then re-run this script."
    exit 1
}
Write-Host "Package removed successfully."

Write-Host "=== 3. Re-registering fresh build ==="
$manifest = "C:\Users\sayan\Documents\GitHub\obs plugin\OBSGameBar\bin\x64\Debug\AppxManifest.xml"
if (Test-Path $manifest) {
    Add-AppxPackage -Register $manifest -ForceApplicationShutdown -ForceUpdateFromAnyVersion
    Write-Host "Registered OK."
} else {
    Write-Host "Manifest not found - build the app first (F5 in Visual Studio), then run this script again."
}

Write-Host "Done. Open Game Bar (Win+G) and check the OBS Studio widget."
