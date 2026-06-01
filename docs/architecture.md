# 架构说明

## 组件

1. `WinNtf.App`
   - WPF 桌面进程。
   - 负责 popup window 和 UI dispatch。
   - 使用最小 system tray 生命周期：显示 alive 状态、提供测试通知/打开配置/退出菜单。
   - 通过当前用户的 Windows Run key 支持登录后自启动，用于覆盖机器重启后的常驻需求。

2. `WinNtf.Server`
   - 本地 HTTP server。
   - 只绑定 `127.0.0.1`。
   - 提供 `GET /health` 与 `POST /notify`。
   - 安全边界是 loopback-only、payload size limit 和队列/频率限制；不增加 token auth。

3. `WinNtf.Core`
   - 纯业务逻辑。
   - 包含通知 payload、normalizer、queue、position calculation、config load/save。
   - 不引用 WPF，便于单元测试。

## 数据流

```text
agent hook
  -> POST /notify
  -> LocalHttpServer
  -> NotificationNormalizer
  -> NotificationQueue
  -> WPF PopupPresenter
```

## 设计取舍

- WPF 原生 UI 足以实现圆角、阴影、左侧颜色条、Topmost、不抢焦点等行为；因此初版不引入 WebView2。
- 旧 prompts repo 的 Browser Notification/SSE 方案提供了 HTTP 端点与自启动思路，但 UI 不符合本项目目标。
- 旧 prompts repo 的 FileWatcher/WPF 方案提供了 WPF popup 与 Win32 style 经验；本项目保留 UI 思路，通信层改成本地 HTTP。


## Popup slot policy

`WinNtf.Core.NotificationSlotQueue` 负责可见槽位分配。默认可见上限来自 `AppConfig.MaxVisible = 10`，`WinNtf.App.App` 启动时把该配置传入 `PopupPresenter`。

当可见 popup 已满时，`PopupPresenter` 调用 `AcquireDroppingOldest()` 获取新 lease：队列关闭最早的 popup，给新通知复用同一 slot。lease 带 generation；旧 popup 的关闭回调如果晚到，只能释放匹配 generation 的 slot，避免 stale callback 把新 popup 的槽位释放掉。

`PopupPresenter` 还会用 `NotificationDisplayKey` 去重仍可见的相同通知。key 由 `title`、`text`、`variant`、`color`、`position` 组成，不包含 `durationMs` / `persistent`；重复通知会复用原 popup、重置倒计时，并调用 `BumpToNewest()` 让该 popup 在 drop 顺序里变成最新。UI 层会重新排列当前可见窗口，把重复项移到栈顶。
