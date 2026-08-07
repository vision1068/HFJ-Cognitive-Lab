<#
    One-click live-mode launcher (owner convenience script - not part of the audited
    product surface; automates the exact manual steps in USER-GUIDE.md Section 4).

    Does three things, in order, and stops with a clear message if any step fails
    rather than silently proceeding to the next one:
      1. Makes sure MetaTrader 5 is running (launches it if not; does NOT touch login -
         you must already be logged into your broker account, same as the manual flow).
      2. Generates a GSA_BRIDGE_TOKEN once and reuses it forever after (persisted at
         User scope), then starts the read-only bridge (run_bridge.py --live) in the
         background and waits until it is actually listening - never launches the app
         against a bridge that is not really up.
      3. Launches the published self-contained exe with that token in its environment.

    Usage:
        Double-click Launch-Live.bat  (preferred - no PowerShell prompt/policy friction)
        or:  powershell -ExecutionPolicy Bypass -File Launch-Live.ps1

    Safety notes (unchanged from the rest of the app):
      - Read-only. This script never touches order placement - it only starts the
        existing FR-36 read-only bridge and the app (INV-1).
      - No credential is generated/stored here beyond the loopback bridge token, which
        is NOT your broker password (NFR-1/NFR-2) - see USER-GUIDE.md Section 4.
      - If MetaTrader 5 is not already logged into your broker, the bridge will fail
        fast with a clear error (see the console output) instead of the app silently
        showing "waiting" forever.
#>
$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot

# ---- configuration -------------------------------------------------------
$mt5Path   = "C:\Program Files\MetaTrader 5\terminal64.exe"   # adjust if your install differs
$bridgeDir = Join-Path $root 'src\bridge'
$publishExe = Join-Path $root 'src\GoldSignalAnalyzer.Wpf\bin\publish\win-x64\GoldSignalAnalyzer.Wpf.exe'
$releaseExe = Join-Path $root 'src\GoldSignalAnalyzer.Wpf\bin\Release\net8.0-windows\GoldSignalAnalyzer.Wpf.exe'
$bridgeUrl  = 'http://127.0.0.1:9001/'
$logDir     = Join-Path $env:TEMP 'GoldSignalAnalyzer-launch-logs'
New-Item -ItemType Directory -Force -Path $logDir | Out-Null

function Fail($msg) { Write-Host "`n[FAILED] $msg" -ForegroundColor Red; exit 1 }

# ---- step 1: MetaTrader 5 --------------------------------------------------
Write-Host "[1/3] Checking MetaTrader 5..." -ForegroundColor Cyan
$mt5 = Get-Process -Name 'terminal64' -ErrorAction SilentlyContinue
if (-not $mt5) {
    if (-not (Test-Path $mt5Path)) {
        Fail "MetaTrader 5 not found at '$mt5Path' and no terminal64 process is running. Edit mt5Path at the top of this script if your install is elsewhere, or launch MT5 and log in manually."
    }
    Write-Host "  MT5 not running - launching $mt5Path" -ForegroundColor Yellow
    Start-Process -FilePath $mt5Path | Out-Null
    Write-Host "  Log into your broker account in the MT5 window that just opened, then re-run this script." -ForegroundColor Yellow
    Write-Host "  Re-running is safe - this script skips this step once MT5 is already running." -ForegroundColor Yellow
    exit 0
}
Write-Host "  MT5 already running (PID $($mt5.Id))." -ForegroundColor Green

# ---- step 2: the read-only bridge -----------------------------------------
Write-Host "[2/3] Starting the read-only MT5 bridge..." -ForegroundColor Cyan

$token = [System.Environment]::GetEnvironmentVariable('GSA_BRIDGE_TOKEN', 'User')
if ([string]::IsNullOrWhiteSpace($token)) {
    Write-Host "  No saved bridge token - generating one (stored for future runs only, never your broker password)." -ForegroundColor Yellow
    $rng = New-Object System.Security.Cryptography.RNGCryptoServiceProvider
    $bytes = New-Object byte[] 32
    $rng.GetBytes($bytes)
    $token = ([Convert]::ToBase64String($bytes) -replace '[+/=]', '').Substring(0, 40)
    [System.Environment]::SetEnvironmentVariable('GSA_BRIDGE_TOKEN', $token, 'User')
}

# Already listening? Do not start a second bridge process on the same port.
$alreadyUp = $false
try {
    Invoke-WebRequest -Uri $bridgeUrl -UseBasicParsing -TimeoutSec 2 -ErrorAction Stop | Out-Null
    $alreadyUp = $true
} catch [System.Net.WebException] {
    if ($_.Exception.Response) { $alreadyUp = $true }  # any HTTP response (e.g. 401) means something is listening
}

if ($alreadyUp) {
    Write-Host "  Bridge already listening on $bridgeUrl - reusing it." -ForegroundColor Green
} else {
    $stdout = Join-Path $logDir 'bridge_stdout.log'
    $stderr = Join-Path $logDir 'bridge_stderr.log'
    Remove-Item $stdout, $stderr -ErrorAction SilentlyContinue

    $psi = New-Object System.Diagnostics.ProcessStartInfo
    $psi.FileName = 'python'
    $psi.Arguments = 'run_bridge.py --live'
    $psi.WorkingDirectory = $bridgeDir
    $psi.UseShellExecute = $false
    $psi.CreateNoWindow = $true
    $psi.RedirectStandardOutput = $true
    $psi.RedirectStandardError = $true
    $psi.EnvironmentVariables['GSA_BRIDGE_TOKEN'] = $token
    $bridgeProc = [System.Diagnostics.Process]::Start($psi)

    # Drain the redirected streams to files asynchronously so the process never blocks
    # on a full pipe buffer, and we can inspect the log if startup fails.
    Start-Job -ScriptBlock {
        param($proc, $outFile, $errFile)
        $proc.StandardOutput.ReadToEnd() | Out-File $outFile -Encoding utf8
    } -ArgumentList $bridgeProc, $stdout, $stderr | Out-Null

    Write-Host "  Waiting for the bridge to come up (PID $($bridgeProc.Id))..." -ForegroundColor Yellow
    $up = $false
    for ($i = 0; $i -lt 15; $i++) {
        Start-Sleep -Seconds 1
        if ($bridgeProc.HasExited) { break }
        try {
            Invoke-WebRequest -Uri $bridgeUrl -UseBasicParsing -TimeoutSec 2 -ErrorAction Stop | Out-Null
            $up = $true; break
        } catch [System.Net.WebException] {
            if ($_.Exception.Response) { $up = $true; break }
        } catch { }
    }

    if ($bridgeProc.HasExited -or -not $up) {
        Fail "Bridge did not come up. Common cause: MT5 is running but not logged into a broker account. Check logs: $stdout and $stderr"
    }
    Write-Host "  Bridge is up on $bridgeUrl (PID $($bridgeProc.Id))." -ForegroundColor Green
}

# ---- step 3: the app --------------------------------------------------------
Write-Host "[3/3] Launching Gold Signal Analyzer..." -ForegroundColor Cyan
$exe = if (Test-Path $publishExe) { $publishExe } elseif (Test-Path $releaseExe) { $releaseExe } else { Fail "No built exe found. Run '.\publish.ps1' first." }

$appPsi = New-Object System.Diagnostics.ProcessStartInfo
$appPsi.FileName = $exe
$appPsi.UseShellExecute = $false
$appPsi.EnvironmentVariables['GSA_BRIDGE_TOKEN'] = $token
[System.Diagnostics.Process]::Start($appPsi) | Out-Null

Write-Host "`nDone. If the dashboard's Data Source is not already 'Live MT5', set it in the wizard/setup once." -ForegroundColor Green
