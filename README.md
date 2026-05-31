# win-ntf

`win-ntf` 是一个 Windows 本地桌面通知程序，目标是给 coding agent hooks 提供可靠、可自定义、无需远程服务器的通知通道。

核心链路：

```text
agent hook -> POST http://127.0.0.1:<port>/notify -> WPF custom popup
```

## 目标

- 本机运行，不依赖远程服务。
- 只监听 loopback 地址。
- 通过 local HTTP call 接收通知。
- 使用 WPF 自绘 popup，不使用 Windows Toast 或 Browser Notification 作为主 UI。
- 支持可测试的 payload normalization、位置计算和通知队列。
- 提供最小 system tray：显示运行状态、测试通知、打开配置目录、退出。
- 默认写入当前用户开机/登录自启动项，配置在 `%APPDATA%/win-ntf/config.json`。

## 非目标

- 不做 WebView2。
- 不做 Electron / Tauri。
- 不做远程 push notification。
- 不要求浏览器页面常驻。
- 初版不支持局域网调用。
- 不做 token auth；安全边界是只监听 `127.0.0.1`。

## 本地开发验证

```bash
export PATH="$HOME/.dotnet:$PATH"
dotnet build WinNtf.sln
dotnet run --project tests/WinNtf.Core.Tests/WinNtf.Core.Tests.csproj
```

## Windows 真实 smoke test

解压 `win-ntf-win-x64.7z` 后，可以直接运行脚本：

```powershell
.\smoke-win.ps1 -ExePath .\win-ntf.exe
```

也可以手动启动 `win-ntf.exe`。启动后，托盘应显示 `win-ntf alive on 127.0.0.1:9876`。

```bash
curl http://127.0.0.1:9876/health

curl -X POST http://127.0.0.1:9876/notify \
  -H 'Content-Type: application/json' \
  -d '{"title":"test","text":"hello from hook","variant":"success"}'
```

验收：

- `/health` 返回 `ok`。
- `/notify` 弹出 WPF popup。
- popup 使用 variant/color，按位置显示，并按 `durationMs` 自动关闭。
- tray 菜单可发送测试通知、打开配置目录、退出程序。
- 退出后端口释放。

## 发布包

GitHub Actions workflow：`.github/workflows/build-windows.yml`

产物：

```text
win-ntf-win-x64.7z
```

手动本地发布（需要 Windows + PowerShell + 7-Zip）：

```powershell
./scripts/package-win-x64.ps1
```

发布脚本会执行 self-contained single-file publish，所以目标 Windows 机器不需要单独安装 .NET Desktop Runtime。

## API

### `GET /health`

返回：

```text
ok
```

### `POST /notify`

```bash
curl -X POST http://127.0.0.1:9876/notify \
  -H 'Content-Type: application/json' \
  -d '{"title":"✅ win-ntf","text":"Task completed","variant":"success"}'
```

更多设计见：

- `docs/architecture.md`
- `docs/api.md`
- `.omx/plans/win-ntf-local-http-wpf-plan.md`
