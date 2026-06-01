# 参考来源

本项目从 `/mnt/d/code/prompts` 的历史通知实现中取思路，但不直接照搬旧通信层。

## prompts repo 证据

- `openspec/specs/desktop-notify-sse/spec.md`
  - 旧 HTTP/SSE server 规格：`/health`、`/events`、`/notify`、`/`。
  - 可借鉴：health check、自启动、local notify endpoint。
  - 不照搬：Browser Notification UI。

- `openspec/specs/desktop-notify-wpf/spec.md`
  - WPF popup 规格：跨虚拟桌面、420x105、圆角、阴影、左侧颜色条、自动关闭、点击关闭。

- `openspec/changes/archive/2026-05-04-upgrade-desktop-notify-wpf/design.md`
  - WPF + Win32 style 思路：`WS_EX_TOOLWINDOW`、移除 `WS_EX_APPWINDOW`。

- `packages/oc-tweaks/src/utils/wpf-notify.ts`
  - 可借鉴：base64 传文本、清理 Markdown、截断、默认 style、`ShowActivated=False`。

- `packages/oc-tweaks/src/utils/wpf-position.ts`
  - 可借鉴：slot allocation、queue、position calculation。

- `packages/oc-tweaks/src/plugins/tool-call-notify.ts`
  - 可借鉴：`maxVisible = 3`、`top-right`、短时 tool-call 通知。


## Agent hook 参考

- opencode: `~/.config/opencode/plugins/win-ntf-notify.ts` 监听 `session.idle`、`session.error`、`permission.asked` / `permission.updated` / `question.asked`；`session.idle` 事件只带 `sessionID`，插件用 `client.session.get({ path: { id } })` 读取 `parentID`，跳过 subagent idle。
- Claude Code: `~/.claude/settings.json` hooks 调用 `~/.claude/hooks/win-ntf-notify.sh`；`Stop` 用作 idle，`StopFailure` 用作 error，`PermissionRequest`/`Notification` 用作 approval，`PreToolUse` + `AskUserQuestion` 用作 question。
- Codex: `~/.codex/hooks.json` 调用 `~/.codex/hooks/win-ntf-notify.sh`；`Stop` 用作 idle，`PermissionRequest` 用作 approval，`PreToolUse` matcher 捕获 question，`PostToolUse` 非零状态近似 error。Codex 当前本地资料未显示专门的 session.error hook 或对话问题专用 hook。
