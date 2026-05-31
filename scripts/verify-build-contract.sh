#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$repo_root"

fail() {
  printf 'FAIL %s\n' "$1" >&2
  exit 1
}

grep -q '^publish-self-contained:' Makefile \
  || fail 'Makefile missing publish-self-contained target'

grep -q '^publish-framework-dependent:' Makefile \
  || fail 'Makefile missing publish-framework-dependent target'

grep -q 'win-ntf-self-contained' Makefile \
  || fail 'Makefile missing self-contained output directory'

grep -q 'win-ntf-framework-dependent' Makefile \
  || fail 'Makefile missing framework-dependent output directory'

grep -q -- '--self-contained false' Makefile \
  || fail 'Makefile missing framework-dependent self-contained override'

grep -q 'PublishSingleFile=false' Makefile \
  || fail 'framework-dependent publish must keep runtimeconfig/deps files beside exe'

grep -q 'ASP.NET Core Runtime' Makefile \
  || fail 'Makefile help must mention ASP.NET Core Runtime for framework-dependent output'

grep -q 'ScreenshotPath' scripts/smoke-win.ps1 \
  || fail 'smoke-win.ps1 missing ScreenshotPath parameter'

grep -q 'DurationMs' scripts/smoke-win.ps1 \
  || fail 'smoke-win.ps1 missing DurationMs parameter'

grep -q 'RemoveTempDir' scripts/smoke-win.ps1 \
  || fail 'smoke-win.ps1 missing RemoveTempDir cleanup option'

grep -q 'Wait-VisiblePopup' scripts/smoke-win.ps1 \
  || fail 'smoke-win.ps1 must wait for a visible popup before screenshot'

grep -q -- '--self-contained true' scripts/package-win-x64.ps1 \
  || fail 'package-win-x64.ps1 must explicitly publish self-contained'

grep -q -- '--runtime win-x64' scripts/package-win-x64.ps1 \
  || fail 'package-win-x64.ps1 must explicitly publish win-x64'

grep -q 'dotnet publish failed' scripts/package-win-x64.ps1 \
  || fail 'package-win-x64.ps1 must fail fast when dotnet publish fails'

grep -q -- '-ScreenshotPath' .github/workflows/build-windows.yml \
  || fail 'Windows workflow smoke must capture screenshots'

grep -q -- '-DurationMs' .github/workflows/build-windows.yml \
  || fail 'Windows workflow smoke must extend popup duration'

printf 'PASS build contract\n'
