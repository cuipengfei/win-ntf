$ErrorActionPreference = "Stop"

$publishDir = "dist/win-ntf"
$archive = "dist/win-ntf-win-x64.7z"

if (Test-Path $publishDir) {
  Remove-Item $publishDir -Recurse -Force
}
if (Test-Path $archive) {
  Remove-Item $archive -Force
}
New-Item -ItemType Directory -Force -Path "dist" | Out-Null

dotnet publish src/WinNtf.App/WinNtf.App.csproj `
  -c Release `
  --runtime win-x64 `
  --self-contained true `
  -p:PublishSingleFile=true `
  -p:EnableCompressionInSingleFile=true `
  -o $publishDir

if ($LASTEXITCODE -ne 0) {
  throw "dotnet publish failed with exit code $LASTEXITCODE"
}

if (-not (Test-Path "$publishDir\win-ntf.exe")) {
  throw "publish did not produce $publishDir\win-ntf.exe"
}

$readme = @"
win-ntf Windows x64 package

Run win-ntf.exe. It starts a local HTTP server on 127.0.0.1 and shows a tray icon.
Config path: %APPDATA%\win-ntf\config.json
Smoke test: curl http://127.0.0.1:9876/health
"@
Set-Content -Path "$publishDir\PACKAGE-README.txt" -Value $readme -Encoding UTF8

if (-not (Get-Command 7z -ErrorAction SilentlyContinue)) {
  if (Get-Command choco -ErrorAction SilentlyContinue) {
    choco install 7zip -y --no-progress
  }
}

if (-not (Get-Command 7z -ErrorAction SilentlyContinue)) {
  throw "7z was not found and could not be installed automatically"
}

Copy-Item -Path "scripts\smoke-win.ps1" -Destination "$publishDir\smoke-win.ps1"
7z a $archive ".\$publishDir\*"
7z l $archive | Select-String "win-ntf.exe" | Out-Null
7z l $archive | Select-String "smoke-win.ps1" | Out-Null
Write-Host "Created $archive"
