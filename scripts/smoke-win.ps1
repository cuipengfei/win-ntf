param(
  [string]$ExePath = ".\win-ntf.exe",
  [int]$Port = 9876,
  [string]$ScreenshotPath = "",
  [int]$DurationMs = 8000,
  [string]$Variant = "success",
  [string]$Title = "win-ntf smoke",
  [string]$Text = "hello from smoke-win.ps1",
  [switch]$RemoveTempDir
)

$ErrorActionPreference = "Stop"

Add-Type @"
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

public static class WinNtfSmokeWindows {
  public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

  [DllImport("user32.dll")]
  public static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

  [DllImport("user32.dll")]
  public static extern bool IsWindowVisible(IntPtr hWnd);

  [DllImport("user32.dll")]
  public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

  [DllImport("user32.dll", CharSet = CharSet.Unicode)]
  public static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);

  public static List<IntPtr> VisibleWindowsForProcess(uint targetProcessId, string title) {
    var windows = new List<IntPtr>();
    EnumWindows((hWnd, lParam) => {
      uint processId;
      GetWindowThreadProcessId(hWnd, out processId);
      if (processId == targetProcessId && IsWindowVisible(hWnd)) {
        var builder = new StringBuilder(256);
        GetWindowText(hWnd, builder, builder.Capacity);
        if (builder.ToString() == title) {
          windows.Add(hWnd);
        }
      }
      return true;
    }, IntPtr.Zero);
    return windows;
  }
}
"@

function Save-Screenshot([string]$Path) {
  Add-Type -AssemblyName System.Windows.Forms
  Add-Type -AssemblyName System.Drawing

  $bounds = [System.Windows.Forms.SystemInformation]::VirtualScreen
  $bitmap = New-Object System.Drawing.Bitmap $bounds.Width, $bounds.Height
  $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
  try {
    $graphics.CopyFromScreen($bounds.Left, $bounds.Top, 0, 0, $bounds.Size)
    $directory = Split-Path -Parent $Path
    if ($directory) {
      New-Item -ItemType Directory -Force -Path $directory | Out-Null
    }
    $bitmap.Save($Path, [System.Drawing.Imaging.ImageFormat]::Png)
  } finally {
    $graphics.Dispose()
    $bitmap.Dispose()
  }
}

function Wait-VisiblePopup([int]$ProcessId) {
  for ($i = 0; $i -lt 40; $i++) {
    $windows = [WinNtfSmokeWindows]::VisibleWindowsForProcess([uint32]$ProcessId, "win-ntf")
    if ($windows.Count -gt 0) {
      Write-Host "Visible popup detected: $($windows[0])"
      return
    }

    Start-Sleep -Milliseconds 250
  }

  throw "visible win-ntf popup did not appear before screenshot"
}

if (-not (Test-Path $ExePath)) {
  throw "win-ntf executable not found: $ExePath"
}

$process = Start-Process -FilePath $ExePath -PassThru
try {
  $healthUrl = "http://127.0.0.1:$Port/health"
  $notifyUrl = "http://127.0.0.1:$Port/notify"

  $ready = $false
  for ($i = 0; $i -lt 30; $i++) {
    try {
      $health = Invoke-RestMethod -Uri $healthUrl -Method Get -TimeoutSec 2
      if ($health -eq "ok") {
        $ready = $true
        break
      }
    } catch {
      Start-Sleep -Milliseconds 500
    }
  }

  if (-not $ready) {
    throw "health endpoint did not return ok within timeout"
  }

  $body = @{ title = $Title; text = $Text; variant = $Variant; durationMs = $DurationMs } | ConvertTo-Json -Compress
  $response = Invoke-WebRequest -UseBasicParsing -Uri $notifyUrl -Method Post -ContentType "application/json" -Body $body -TimeoutSec 5
  if ($response.StatusCode -ne 202) {
    throw "notify endpoint returned $($response.StatusCode), expected 202"
  }

  if ($ScreenshotPath -ne "") {
    Wait-VisiblePopup $process.Id
    Save-Screenshot $ScreenshotPath
    if (-not (Test-Path $ScreenshotPath)) {
      throw "screenshot was not created: $ScreenshotPath"
    }
    if ((Get-Item $ScreenshotPath).Length -le 0) {
      throw "screenshot is empty: $ScreenshotPath"
    }
    Write-Host "Screenshot saved to $ScreenshotPath"
  }

  Write-Host "PASS health and notify smoke test. Visually confirm tray icon and popup appeared."
} finally {
  if (-not $process.HasExited) {
    try { $process.CloseMainWindow() | Out-Null } catch { }
    Start-Sleep -Seconds 2
    if (-not $process.HasExited) {
      Stop-Process -Id $process.Id -Force
      $process.WaitForExit(5000)
    }
  }

  if ($RemoveTempDir) {
    $resolvedExe = Resolve-Path $ExePath
    $candidate = Split-Path -Parent $resolvedExe
    if ((Split-Path -Leaf $candidate) -like "wnt-*") {
      Remove-Item $candidate -Recurse -Force
      Write-Host "Removed temp directory $candidate"
    }
  }
}
