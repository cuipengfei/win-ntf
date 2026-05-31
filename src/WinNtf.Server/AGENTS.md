# WinNtf.Server

loopback-only HTTP 层，依赖 `Core`。把 HTTP 请求翻译成 `NormalizedNotification` 后交给 `INotificationSink`。

## 在哪里找

| 任务 | 位置 |
|------|------|
| 路由 `/health` `/notify`、body 限额、JSON converter | [LocalHttpServer.cs](src/WinNtf.Server/LocalHttpServer.cs) |
| sink 抽象（由 App 的 PopupPresenter 实现） | [INotificationSink.cs](src/WinNtf.Server/INotificationSink.cs) |
| host/port 选项 | [LocalHttpServerOptions.cs](src/WinNtf.Server/LocalHttpServerOptions.cs) |

## 关键事实（改动前先读）

- 构造函数**强制** `options.Host == "127.0.0.1"`，否则抛 `ArgumentException`。这是安全边界，勿放宽。
- `MaxRequestBodyBytes = 16 * 1024`（16KB），轻量 loopback server 超限返回 `413`。
- 用 `TcpListener` 实现最小 HTTP/1.1 处理，只支持 `/health` 与 `/notify`；不要引回 ASP.NET Core/Kestrel，避免为本地两条路由拉入整套 Web host。
- 两个自定义 JSON converter：`KebabCasePositionJsonConverter`（接受 `top-right`/`TopRight` 双写）+ `JsonStringEnumConverter(allowIntegerValues: false)`（variant 只收字符串名）。
- `/notify` 成功返回 `202 Accepted`；捕获 `NotificationValidationException` 返回 `400 { error }`。
- 校验逻辑不在此层——委托给 `Core` 的 `NotificationNormalizer`。

## 反模式

- 禁止新增 token auth、远程绑定、CLI/IPC 端点。安全模型 = loopback-only。
