param(
  [string]$ExePath = ".\win-ntf.exe",
  [int]$Port = 9876
)

$ErrorActionPreference = "Stop"

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

  $body = @{ title = "win-ntf smoke"; text = "hello from smoke-win.ps1"; variant = "success" } | ConvertTo-Json -Compress
  $response = Invoke-WebRequest -UseBasicParsing -Uri $notifyUrl -Method Post -ContentType "application/json" -Body $body -TimeoutSec 5
  if ($response.StatusCode -ne 202) {
    throw "notify endpoint returned $($response.StatusCode), expected 202"
  }

  Write-Host "PASS health and notify smoke test. Visually confirm tray icon and popup appeared."
} finally {
  if (-not $process.HasExited) {
    try { $process.CloseMainWindow() | Out-Null } catch { }
    Start-Sleep -Seconds 2
    if (-not $process.HasExited) {
      Stop-Process -Id $process.Id -Force
    }
  }
}
