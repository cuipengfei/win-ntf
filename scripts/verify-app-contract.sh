#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$repo_root"

fail() {
  printf 'FAIL %s\n' "$1" >&2
  exit 1
}

if grep -q 'SystemIcons.Application' src/WinNtf.App/TrayController.cs; then
  fail 'tray icon must not use the generic SystemIcons.Application icon'
fi

grep -q 'TrayIcon.Create' src/WinNtf.App/TrayController.cs \
  || fail 'TrayController must use the project tray icon'

grep -q 'class TrayIcon' src/WinNtf.App/TrayIcon.cs \
  || fail 'TrayIcon helper must exist'

printf 'PASS app contract\n'
