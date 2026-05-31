#!/usr/bin/env bash
set -euo pipefail

curl -X POST "http://127.0.0.1:${WIN_NTF_PORT:-9876}/notify" \
  -H 'Content-Type: application/json' \
  -d '{"title":"✅ win-ntf","text":"Task completed","variant":"success","durationMs":10000}'
