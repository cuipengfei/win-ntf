# Agent hooks 集成

`win-ntf` 的集成面很小：coding agent 在需要用户注意时，向本机 `http://127.0.0.1:9876/notify` 发送 JSON。

## 共同约定

推荐所有 agent 使用相同语义：

| 语义 | win-ntf variant | 说明 |
| --- | --- | --- |
| idle | `info` | agent 已停止输出，等待用户下一步输入 |
| error | `error` | 会话或工具执行出错 |
| approval | `warning` | agent 阻塞，等待权限审批或确认 |
| question | `warning` | agent 阻塞，等待用户回答问题 |

推荐 payload：

```json
{
  "title": "oc: project-name",
  "text": "空闲，等待你的输入",
  "variant": "info",
  "durationMs": 90000
}
```

`durationMs: 90000` 会让 agent hook 通知默认停留 90 秒；如需手动关闭，可在 popup 右上角点击 `×`。不要为这些 hook 发送 `persistent: true`，否则通知不会自动关闭。

如果同一个仍可见通知再次到达，`win-ntf` 会复用已有 popup：不新建第二个窗口，重置倒计时，并把该 popup 移到栈顶。重复判断使用 `title`、`text`、`variant`、`color`、`position`；`durationMs` 不参与重复判断。

## opencode

本机配置使用用户级插件 `~/.config/opencode/plugins/win-ntf-notify.ts`。如果你的 opencode 配置已经通过 plugin loader 自动加载该目录，把下面文件放进去即可；如果没有 loader，需要按你的 opencode 版本把该插件加入 opencode plugin 配置。

当前行为：

| opencode event | variant | 文案 |
| --- | --- | --- |
| `session.idle` | `info` | `空闲，等待你的输入`；仅 root session 触发，子会话通过 `Session.parentID` 过滤 |
| `session.error` | `error` | `会话出错` |
| `permission.asked` / `permission.updated` / `question.asked` | `warning` | `等待审批 / 需要确认` |

完整插件：

```ts
import type { Plugin } from "@opencode-ai/plugin"

const NOTIFY_URL = "http://127.0.0.1:9876/notify"

type Variant = "info" | "success" | "warning" | "error" | "tool"

interface NotifyPayload {
  title: string
  text: string
  variant: Variant
  durationMs: number
}

export const WinNtfNotifyPlugin: Plugin = async ({ directory, project, client }) => {
  const projectName = pickProjectName(directory, project)

  async function notify(variant: Variant, headline: string, detail?: string) {
    const text = detail && detail.trim().length > 0
      ? `${headline}\n${detail.trim()}`
      : headline

    const payload: NotifyPayload = {
      title: `oc: ${projectName}`,
      text,
      variant,
      durationMs: 90_000,
    }

    try {
      await fetch(NOTIFY_URL, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(payload),
        signal: AbortSignal.timeout(3000),
      })
    } catch {
      // win-ntf 未运行时静默忽略，不能打断 agent 会话。
    }
  }

  return {
    event: async ({ event }: { event: { type: string; properties?: unknown } }) => {
      const type = event?.type
      const props = (event?.properties ?? {}) as Record<string, unknown>

      if (type === "session.idle") {
        if (await isChildSession(client, props)) return
        await notify("info", "空闲，等待你的输入")
        return
      }

      if (type === "session.error") {
        await notify("error", "会话出错", extractErrorDetail(props))
        return
      }

      if (
        type === "permission.asked" ||
        type === "permission.updated" ||
        type === "question.asked"
      ) {
        await notify("warning", "等待审批 / 需要确认", extractApprovalDetail(props))
      }
    },
  }
}

async function isChildSession(
  client: { session?: { get?: (options: { path: { id: string } }) => Promise<{ data?: { parentID?: string } }> } },
  props: Record<string, unknown>,
): Promise<boolean> {
  const sessionID = props.sessionID
  if (typeof sessionID !== "string" || sessionID.trim().length === 0) return false

  try {
    const result = await client.session?.get?.({ path: { id: sessionID } })
    return typeof result?.data?.parentID === "string" && result.data.parentID.length > 0
  } catch {
    return false
  }
}

function pickProjectName(directory: string, project: unknown): string {
  const p = project as { id?: string; worktree?: string } | undefined
  const base = p?.worktree ?? directory ?? ""
  const normalized = base.replace(/\\/g, "/")
  const segments = normalized.split("/").filter(Boolean)
  return segments[segments.length - 1] || "opencode"
}

function extractErrorDetail(props: Record<string, unknown>): string | undefined {
  const error = props.error as { name?: string; data?: { message?: string } } | undefined
  if (!error) return undefined
  return error.data?.message ?? error.name ?? undefined
}

function extractApprovalDetail(props: Record<string, unknown>): string | undefined {
  const title = props.title
  if (typeof title === "string" && title.trim().length > 0) return title
  const permission = props.permission
  if (typeof permission === "string" && permission.trim().length > 0) return permission
  return undefined
}
```

验证插件能被打包：

```bash
bun build ~/.config/opencode/plugins/win-ntf-notify.ts --target=bun --outfile=/tmp/win-ntf-notify-check.js
```

## Claude Code

Claude Code 使用 `~/.claude/settings.json` hooks 调用脚本，例如 `~/.claude/hooks/win-ntf-notify.sh`。

当前行为：

| Claude Code hook | 脚本参数 | 语义 |
| --- | --- | --- |
| `Stop` | `idle` | 等待用户输入 |
| `StopFailure` | `error` | 会话停止失败或 hook 失败 |
| `PermissionRequest` / permission notification | `approve` | 等待审批 |
| `PreToolUse` matcher `AskUserQuestion` | `question` | 等待用户回答 |

脚本 `~/.claude/hooks/win-ntf-notify.sh`：

```bash
#!/bin/bash
set -euo pipefail

NOTIFY_URL="http://127.0.0.1:9876/notify"
TYPE="${1:-}"
INPUT=$(cat)

PROJECT=$(echo "$INPUT" | jq -r '.cwd // ""' 2>/dev/null | xargs basename 2>/dev/null || true)
PROJECT="${PROJECT:-claude-code}"

VARIANT="info"
HEADLINE=""
DETAIL=""

case "$TYPE" in
  idle)
    VARIANT="info"
    HEADLINE="空闲，等待你的输入"
    ;;
  error)
    VARIANT="error"
    HEADLINE="会话出错"
    DETAIL=$(echo "$INPUT" | jq -r '.error // .error_message // (.tool_input.error // empty) // ""' 2>/dev/null | tr -d '\n')
    ;;
  approve)
    VARIANT="warning"
    HEADLINE="等待审批 / 需要确认"
    DETAIL=$(echo "$INPUT" | jq -r '.message // ""' 2>/dev/null | tr -d '\n')
    ;;
  question)
    VARIANT="warning"
    HEADLINE="需要你回答问题"
    DETAIL=$(echo "$INPUT" | jq -r '.tool_input.question // .message // ""' 2>/dev/null | tr -d '\n')
    ;;
  *)
    exit 0
    ;;
esac

if [ -n "$DETAIL" ]; then
  TEXT="${HEADLINE}\n${DETAIL}"
else
  TEXT="$HEADLINE"
fi

curl -s --max-time 3 -X POST "$NOTIFY_URL" \
  -H "Content-Type: application/json" \
  -d "$(jq -n \
    --arg title "cc: $PROJECT" \
    --arg text "$TEXT" \
    --arg variant "$VARIANT" \
    '{title: $title, text: $text, variant: $variant, durationMs: 90000}'
  )" >/dev/null 2>&1 || true

exit 0
```

让脚本可执行：

```bash
chmod +x ~/.claude/hooks/win-ntf-notify.sh
```

`~/.claude/settings.json` 片段。不要直接覆盖整个文件；把这些 hook 合并进现有配置：

```json
{
  "hooks": {
    "Stop": [
      {
        "hooks": [
          { "type": "command", "command": "$HOME/.claude/hooks/win-ntf-notify.sh idle", "timeout": 10 }
        ]
      }
    ],
    "StopFailure": [
      {
        "hooks": [
          { "type": "command", "command": "$HOME/.claude/hooks/win-ntf-notify.sh error", "timeout": 10 }
        ]
      }
    ],
    "Notification": [
      {
        "matcher": "permission_prompt",
        "hooks": [
          { "type": "command", "command": "$HOME/.claude/hooks/win-ntf-notify.sh approve", "timeout": 10 }
        ]
      }
    ],
    "PreToolUse": [
      {
        "matcher": "AskUserQuestion",
        "hooks": [
          { "type": "command", "command": "$HOME/.claude/hooks/win-ntf-notify.sh question", "timeout": 10 }
        ]
      }
    ]
  }
}
```

注意：`Notification` 的 `idle_prompt` 在实测中不能可靠代表“Claude 已经等待用户输入”；`Stop` 更接近这个语义。如果你同时配置 `Notification` 的 `idle_prompt` 和 `Stop`，可能会收到重复 idle；`win-ntf` 会去重，但更推荐只保留 `Stop` idle。

验证：

```bash
bash -n ~/.claude/hooks/win-ntf-notify.sh
jq empty ~/.claude/settings.json
printf '{"cwd":"/tmp/project","tool_input":{"question":"继续吗"}}' | ~/.claude/hooks/win-ntf-notify.sh question
```

## Codex

Codex 使用 `~/.codex/hooks.json`。本机配置追加脚本 `~/.codex/hooks/win-ntf-notify.sh`，不覆盖 oh-my-codex 和 plannotator 既有 hook。

当前行为：

| Codex hook | matcher | 脚本参数 | 语义 |
| --- | --- | --- | --- |
| `Stop` | 无 | `idle` | 等待用户输入 |
| `PermissionRequest` | 无 | `approve` | 等待审批 |
| `PreToolUse` | `AskUserQuestion|question|ask_user_question` | `question` | 等待用户回答 |
| `PostToolUse` | 无 | `error` | 工具非零退出或 stderr，近似错误通知 |

Codex 本地 hook 资料中没有独立的 `session.error` 事件，也没有对话问题专用 hook；因此 error 使用 `PostToolUse` 的非零状态或 stderr 近似，question 只能通过 `PreToolUse` matcher 捕获工具化的提问。脚本对无错误的 `PostToolUse` 直接 `exit 0`，不会给成功工具调用发噪声通知。

脚本 `~/.codex/hooks/win-ntf-notify.sh`：

```bash
#!/bin/bash
set -euo pipefail

NOTIFY_URL="http://127.0.0.1:9876/notify"
TYPE="${1:-}"
INPUT=$(cat)

json_get() {
  jq -r "$1" 2>/dev/null <<<"$INPUT" || true
}

PROJECT=$(json_get '.cwd // ""' | xargs basename 2>/dev/null || true)
PROJECT="${PROJECT:-codex}"

EVENT=$(json_get '.hook_event_name // .event // .type // ""')
TOOL=$(json_get '.tool_name // .toolName // .tool // .name // .tool_input.name // ""')

VARIANT="info"
HEADLINE=""
DETAIL=""

case "$TYPE" in
  idle)
    VARIANT="info"
    HEADLINE="空闲，等待你的输入"
    ;;
  approve)
    VARIANT="warning"
    HEADLINE="等待审批 / 需要确认"
    DETAIL=$(json_get '.tool_name // .toolName // .tool // .permission // .message // ""' | tr -d '\n')
    ;;
  question)
    case "${EVENT}:${TOOL}" in
      *AskUserQuestion*|*ask_user_question*|*question*) ;;
      *) exit 0 ;;
    esac
    VARIANT="warning"
    HEADLINE="需要你回答问题"
    DETAIL=$(json_get '.question // .tool_input.question // .message // .prompt // ""' | tr -d '\n')
    ;;
  error)
    STATUS=$(json_get '.exit_code // .exitCode // .status // .tool_response.exit_code // ""')
    ERROR=$(json_get '.error // .error_message // .stderr // .tool_response.stderr // ""' | tr -d '\n')
    if [ -z "$STATUS$ERROR" ] || [ "$STATUS" = "0" ]; then
      exit 0
    fi
    VARIANT="error"
    HEADLINE="会话出错"
    DETAIL="${ERROR:-exit_code=$STATUS}"
    ;;
  *)
    exit 0
    ;;
esac

if [ -n "$DETAIL" ]; then
  TEXT="${HEADLINE}\n${DETAIL}"
else
  TEXT="$HEADLINE"
fi

curl -s --max-time 3 -X POST "$NOTIFY_URL" \
  -H "Content-Type: application/json" \
  -d "$(jq -n \
    --arg title "cx: $PROJECT" \
    --arg text "$TEXT" \
    --arg variant "$VARIANT" \
    '{title: $title, text: $text, variant: $variant, durationMs: 90000}'
  )" >/dev/null 2>&1 || true

exit 0
```

让脚本可执行：

```bash
chmod +x ~/.codex/hooks/win-ntf-notify.sh
```

`~/.codex/hooks.json` 片段。不要直接覆盖整个文件；把下面条目追加/合并到现有事件数组中：

```json
{
  "hooks": {
    "Stop": [
      { "hooks": [{ "type": "command", "command": "/home/me/.codex/hooks/win-ntf-notify.sh idle", "timeout": 10 }] }
    ],
    "PermissionRequest": [
      { "hooks": [{ "type": "command", "command": "/home/me/.codex/hooks/win-ntf-notify.sh approve", "timeout": 10 }] }
    ],
    "PreToolUse": [
      { "matcher": "AskUserQuestion|question|ask_user_question", "hooks": [{ "type": "command", "command": "/home/me/.codex/hooks/win-ntf-notify.sh question", "timeout": 10 }] }
    ],
    "PostToolUse": [
      { "hooks": [{ "type": "command", "command": "/home/me/.codex/hooks/win-ntf-notify.sh error", "timeout": 10 }] }
    ]
  }
}
```

确保 Codex hook 功能开启。`~/.codex/config.toml` 应有：

```toml
[features]
hooks = true
```

如果 Codex 提示 hook 未信任，进入 Codex 后运行 `/hooks`，信任新增的 win-ntf hook 命令。某些 Codex 版本会在 `hooks.json` 或 `config.toml` 里记录 trusted hash；不要手写猜 hash，优先用 `/hooks` 信任。

验证：

```bash
bash -n ~/.codex/hooks/win-ntf-notify.sh
jq empty ~/.codex/hooks.json
python3 - <<'PY'
import tomllib
tomllib.load(open('/home/me/.codex/config.toml', 'rb'))
PY
printf '{"cwd":"/tmp/project","hook_event_name":"Stop"}' | ~/.codex/hooks/win-ntf-notify.sh idle
```

## 最小端到端验证

先确认 `win-ntf` 正在运行：

```bash
curl http://127.0.0.1:9876/health
```

应该返回：

```text
ok
```

再发送一条通用通知：

```bash
curl -X POST http://127.0.0.1:9876/notify \
  -H 'Content-Type: application/json' \
  -d '{"title":"hook test","text":"hello from agent hook","variant":"info","durationMs":90000}'
```

如果你连发两次完全相同 payload，应只看到一个 popup 被刷新/移到栈顶，而不是两个重复 popup。
