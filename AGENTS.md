# 项目知识库

**生成时间：** 2026-05-31
**Commit：** 49ee257
**分支：** master

## 概览

`win-ntf` 是 .NET 8 Windows 桌面通知程序，给 coding agent hooks 提供本地通知通道。链路：`agent hook -> POST 127.0.0.1:<port>/notify -> WPF 自绘 popup`。四个项目分层：纯逻辑 `Core`、loopback HTTP `Server`、WPF/托盘 `App`、自定义测试运行器 `Tests`。

## 结构

```
win-ntf/
├── src/
│   ├── WinNtf.Core/      # 纯业务逻辑，不引用 WPF（可单测）
│   ├── WinNtf.Server/    # loopback-only HTTP，依赖 Core
│   └── WinNtf.App/       # WPF popup + 托盘，依赖 Core + Server
├── tests/WinNtf.Core.Tests/  # 自定义可执行测试运行器（非 xUnit）
├── docs/                 # api.md / architecture.md / references.md
├── scripts/              # package-win-x64.ps1 / smoke-win.ps1 / visual-curl.sh
└── examples/             # curl 用例
```

依赖方向：`App -> Server -> Core`。Core 永不反向引用上层，永不引用 WPF。

## 在哪里找

| 任务 | 位置 |
|------|------|
| 改 payload 字段/默认值/校验 | [NotificationNormalizer.cs](src/WinNtf.Core/NotificationNormalizer.cs) |
| 改 HTTP 路由/限额/JSON 转换 | [LocalHttpServer.cs](src/WinNtf.Server/LocalHttpServer.cs) |
| 改 popup 外观/堆叠/关闭 | [PopupWindow.xaml.cs](src/WinNtf.App/PopupWindow.xaml.cs) |
| 改弹窗位置算法 | [PositionCalculator.cs](src/WinNtf.Core/Positioning/PositionCalculator.cs) |
| 改托盘菜单/启动流程 | [TrayController.cs](src/WinNtf.App/TrayController.cs) · [App.xaml.cs](src/WinNtf.App/App.xaml.cs) |
| 改配置项/持久化 | [AppConfig.cs](src/WinNtf.Core/AppConfig.cs) · [AppConfigStore.cs](src/WinNtf.Core/AppConfigStore.cs) |
| 加测试 | [Tests/Program.cs](tests/WinNtf.Core.Tests/Program.cs)（必须手动注册） |

## 命令

`dotnet` 已加入 PATH（`~/.dotnet`，见 `.zshrc`）。首选 `make`，它自带 dotnet 路径兜底：

```bash
make build                        # 编译解决方案（CONFIG=Release）
make test                         # 跑自定义测试运行器（18 个测试）
make publish-self-contained       # 发布自包含 Windows x64 产物到 dist/win-ntf-self-contained
make publish-framework-dependent  # 发布框架依赖 Windows x64 产物到 dist/win-ntf-framework-dependent（需 .NET Desktop Runtime）
make help                         # 列出所有命令
scripts/visual-curl.sh            # 对运行中的 app 做手动视觉检查
```

等价裸命令：`dotnet build WinNtf.sln -c Release` / `dotnet run --project tests/WinNtf.Core.Tests/WinNtf.Core.Tests.csproj -c Release`。

Windows 打包（需 PowerShell + 7-Zip）：`./scripts/package-win-x64.ps1` 显式执行 self-contained win-x64 publish，产出 `dist/win-ntf/` 与自包含单文件压缩包 `win-ntf-win-x64.7z`，目标机无需装 .NET Desktop Runtime。`make publish-self-contained` 与 `make publish-framework-dependent` 两个发布目录也都会带 `smoke-win.ps1`；framework-dependent 目标机需 .NET Desktop Runtime。真实验收用 Windows host 跑 `-ScreenshotPath` 与加长 `-DurationMs`，脚本会确认可见 popup 后截图。

## 约定

- C# nullable reference types + implicit usings（项目文件已配）。
- 优先 small sealed 类型、显式领域命名、直白控制流。
- 命名：public 类型/成员 PascalCase，局部/参数 camelCase。
- UI 留在 `App`，HTTP 留在 `Server`，纯逻辑留在 `Core`——不要跨层混。

## 反模式（本项目硬约束）

服务器**必须**只绑定 `127.0.0.1`。除非显式重开 scope，**禁止**新增：

- 远程访问 / 局域网调用
- token auth（安全边界就是 loopback-only）
- CLI / IPC 扩展
- WebView2 / Electron / Tauri
- Windows Toast 作为主 UI（用 WPF 自绘）

Popup **必须**非侵入：每条通知带 `×` 关闭控件，且**不抢前台焦点**（last commit 验证过 foreground handle 不变）。

## 提交规范

Lore 风格：一行 intent + trailers。常用 trailer：`Constraint:`、`Rejected:`（带理由）、`Directive:`、`Confidence:`、`Scope-risk:`、`Tested:`、`Not-tested:`。PR 需附行为摘要、验证输出，UI 改动附截图/说明。
