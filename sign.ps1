# sign.ps1 - Signs a built MSIX package with the trusted OBS Game Bar developer
# certificate (CN=OBSGameBarWidget) from the current user's certificate store.
#
# Why it works this way:
#   * The previous build_and_run.bat generated a fresh self-signed "CN=Antigravity"
#     cert on every run via New-SelfSignedCertificate. Windows SignTool rejects those
#     with 0x8007000B ("SignerSign() failed" / ERROR_BAD_FORMAT), so signing always
#     failed. A properly-issued developer cert (CN=OBSGameBarWidget) is already in the
#     user's store and trusted, so we use that one.
#   * This script is launched by a .bat, i.e. powershell.exe is spawned from cmd.exe.
#     In that context Windows PowerShell's virtual "cert:" drive may not exist
#     ("Cannot find drive ... 'cert'"), so we must NOT rely on Get-ChildItem cert:
#     to pick the cert. Instead we pass the certificate thumbprint directly to
#     signtool (/sha1 ... /s my), which reads the OS store via native Windows APIs.
#     When the cert: drive IS available we still use it to refresh the thumbprint
#     dynamically; otherwise we fall back to the known-good thumbprint below.
#
# Usage:
#   powershell -NoProfile -ExecutionPolicy Bypass -File sign.ps1 "<msix>" "<signtool>"

param(
    [string]$MsixPath,
    [string]$SignTool
)

$ErrorActionPreference = 'Stop'

# Known-good, trusted developer certificate: CN=OBSGameBarWidget.
# Update these if you ever install a replacement signing certificate.
$PreferredCertSubject  = 'OBSGameBarWidget'
$FallbackCertThumb     = '60D73569B97B2971E4C95D86C9A305712AB5BF35'
$SignThumb             = ''

if (-not $MsixPath -or -not (Test-Path $MsixPath)) {
    Write-Error "MSIX package not found: '$MsixPath'"
    exit 1
}
if (-not $SignTool -or -not (Test-Path $SignTool)) {
    Write-Error "signtool not found: '$SignTool'"
    exit 1
}

# Optional dynamic lookup - works where Windows PowerShell's cert: drive is present.
try {
    $found = Get-ChildItem 'cert:\CurrentUser\My' -ErrorAction Stop |
        Where-Object { $_.HasPrivateKey -and $_.Subject -like ("*" + $PreferredCertSubject + "*") } |
        Sort-Object NotAfter -Descending |
        Select-Object -First 1
    if ($found) {
        $SignThumb = $found.Thumbprint
        Write-Host ("Discovered signing certificate: " + $found.Subject + " (" + $SignThumb + ")")
    }
}
catch {
    Write-Host ("PowerShell cert: store unavailable (" + $_.Exception.Message + ") - using pinned thumbprint.")
}

if (-not $SignThumb) {
    if ($FallbackCertThumb) {
        $SignThumb = $FallbackCertThumb
        Write-Host ("Using pinned signing certificate thumbprint: " + $SignThumb + " (CN=" + $PreferredCertSubject + ")")
    }
    else {
        Write-Error "No signing certificate thumbprint available."
        exit 1
    }
}

Write-Host ""
Write-Host ("Signing package : " + $MsixPath)
Write-Host ("      certificate: " + $SignThumb)
Write-Host ""

# Sign with the specific certificate from the user store (/sha1 pins it), so we
# never accidentally use one of the rejected CN=Antigravity certificates.
& $SignTool sign /fd SHA256 /a /sha1 $SignThumb /s my "$MsixPath"
$code = $LASTEXITCODE

if ($code -ne 0) {
    Write-Host ""
    Write-Host ("Signature with pinned certificate failed (" + $code + ") - retrying with auto-selection.")
    & $SignTool sign /fd SHA256 /a "$MsixPath"
    $code = $LASTEXITCODE
}

exit $code