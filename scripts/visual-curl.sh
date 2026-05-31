#!/usr/bin/env bash
set -euo pipefail

base_url="http://127.0.0.1:${WIN_NTF_PORT:-9876}"

send() {
  local label="$1"
  local payload="$2"
  printf '\n--- %s ---\n' "$label"
  curl -i -sS -X POST "$base_url/notify" \
    -H 'Content-Type: application/json' \
    -d "$payload" | sed -n '1,8p'
}

send "info top-right, 30s" '{"title":"ℹ️ 30s Info","text":"Has an × close button; should not steal focus.","variant":"info","position":"top-right","durationMs":30000}'
sleep 0.7
send "success bottom-right, 30s" '{"title":"✅ Bottom Right","text":"Kebab-case position: bottom-right.","variant":"success","position":"bottom-right","durationMs":30000}'
sleep 0.7
send "error top-right, 45s" '{"title":"❌ Error","text":"Long-lived error style; close with × whenever you want.","variant":"error","position":"top-right","durationMs":45000}'
sleep 0.7
send "persistent center" '{"title":"📌 Persistent Center","text":"persistent=true; should stay until you click ×.","variant":"warning","position":"center","persistent":true}'
