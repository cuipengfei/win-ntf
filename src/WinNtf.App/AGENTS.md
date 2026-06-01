# WinNtf.App

WPF 桌面进程，依赖 `Core` + `Server`。唯一允许引用 WPF 的层。承载 popup UI、托盘生命周期、单实例守卫、登录自启动。

## 在哪里找

| 任务 | 位置 |
|------|------|
| 启动编排（单实例→配置→server→tray） | [App.xaml.cs](src/WinNtf.App/App.xaml.cs) |
| popup 外观/颜色条/×关闭/自动关闭计时 | [PopupWindow.xaml.cs](src/WinNtf.App/PopupWindow.xaml.cs) · [PopupWindow.xaml](src/WinNtf.App/PopupWindow.xaml) |
| sink 实现 + 槽位调度 + Dispatcher 封送 | [PopupPresenter.cs](src/WinNtf.App/PopupPresenter.cs) |
| 托盘菜单（测试通知/打开配置/退出） | [TrayController.cs](src/WinNtf.App/TrayController.cs) |
| 单实例互斥 | [SingleInstanceGuard.cs](src/WinNtf.App/SingleInstanceGuard.cs) |
| HKCU Run 自启动写入 | [RegistryStartupManager.cs](src/WinNtf.App/RegistryStartupManager.cs) |
| 启动失败兜底窗口 | [StartupFailureWindow.xaml.cs](src/WinNtf.App/StartupFailureWindow.xaml.cs) |

## 关键事实（改动前先读）

- `OnStartup`：先 `SingleInstanceGuard` 判主实例，非主则 `Shutdown()` 退出；整个启动包在 try/catch 里，异常弹 `StartupFailureWindow` 而非崩溃。
- `PopupPresenter` 实现 `INotificationSink`，默认 `maxVisible=10`，启动时使用 `AppConfig.MaxVisible`；通过 `NotificationSlotQueue` 取槽，满了关闭最早 popup 并显示新通知。仍可见的相同通知按 `NotificationDisplayKey` 去重，复用 popup、重置 timer、移到栈顶。show 必须经 `System.Windows.Application.Current.Dispatcher.InvokeAsync` 封送到 UI 线程。
- popup 关闭时通过带 generation 的回调 `_slots.Release(slot, generation)` 归还槽位，避免已被顶掉的旧 popup stale callback 释放新 popup 槽位。
- 自启动指向 `Environment.ProcessPath` 真实路径——**禁止**指向 temp publish 路径（last commit 因 stale HKCU Run 项踩过坑）。
- `OnExit` 给 server `StopAsync` 2 秒超时后 `DisposeAsync`。

## 反模式

- popup **必须**非侵入：带 `×` 关闭控件，且**不抢前台焦点**（已验证 foreground handle 不变）。
- 禁止用 Windows Toast 作为主 UI——全部 WPF 自绘。
- 业务逻辑（校验/位置/默认值）属于 `Core`，勿在此层重复。
