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
make build
make test
```

## 发布方式

仓库提供两种 Windows x64 发布产物：

```bash
make publish-self-contained       # 输出 dist/win-ntf-self-contained，目标机无需预装 .NET
make publish-framework-dependent  # 输出 dist/win-ntf-framework-dependent，目标机需预装 .NET Desktop Runtime + ASP.NET Core Runtime
```

`self-contained` 体积更大，适合直接发给没有 .NET 的 Windows 机器。`framework-dependent` 体积更小，只适合已安装匹配 .NET Desktop Runtime **和 ASP.NET Core Runtime** 的机器；本程序用 WPF 显示 popup，同时用 Kestrel 提供本地 HTTP 端点，所以两个 shared runtime 都需要。

`scripts/package-win-x64.ps1` 仍保留为压缩包入口：它显式执行 self-contained win-x64 publish，输出 `dist/win-ntf/` 并打包成 `dist/win-ntf-win-x64.7z`，供 CI artifact 使用。

## Windows 真实 smoke test

对 `make publish-*` 生成的目录，进入对应目录后运行：

```powershell
.\smoke-win.ps1 -ExePath .\win-ntf.exe -ScreenshotPath .\smoke.png -DurationMs 15000
```

解压 `win-ntf-win-x64.7z` 后同样可以在解压目录运行这条命令。脚本会启动程序、等待 `/health` 返回 `ok`、发送 `/notify`、确认当前进程出现可见 `win-ntf` popup 后截图，并在结束时关闭本次启动的进程。`DurationMs` 可调长弹窗停留时间，避免截图太晚错过 popup。`-RemoveTempDir` 只会删除 exe 所在且目录名匹配 `wnt-*` 的测试临时目录；正式 `dist/win-ntf-*` 发布目录不会被它删除。

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
