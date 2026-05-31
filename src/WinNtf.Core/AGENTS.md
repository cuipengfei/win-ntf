# WinNtf.Core

纯业务逻辑，不引用 WPF。所有逻辑在此单测。`App -> Server -> Core`，本层永不反向依赖。

## 在哪里找

| 任务 | 位置 |
|------|------|
| 校验/默认值/markdown 清洗/截断 | [NotificationNormalizer.cs](src/WinNtf.Core/NotificationNormalizer.cs) |
| 弹窗位置算法（center/top-right/bottom-right） | [Positioning/PositionCalculator.cs](src/WinNtf.Core/Positioning/PositionCalculator.cs) |
| 可见槽位分配/回收 | [NotificationSlotQueue.cs](src/WinNtf.Core/NotificationSlotQueue.cs) |
| 配置 load/save/校验 + 默认路径 | [AppConfigStore.cs](src/WinNtf.Core/AppConfigStore.cs) · [AppConfig.cs](src/WinNtf.Core/AppConfig.cs) |

## 领域规则（改动前先读）

- `NotificationNormalizer`：`text` 必填（空白抛 `NotificationValidationException`）；默认 title `win-ntf`，默认 duration `10000ms`，`MaxTextLength=400`（超出加 `...`）。`persistent=true` 强制 duration=0。`durationMs<0` 抛错。
- variant 默认色硬编码在 `DefaultColorFor`：success `#4ADE80`、warning `#FBBF24`、error `#EF4444`、tool/info `#60A5FA`。`color` 只接受 `^#[0-9a-fA-F]{6}$`，否则回退 variant 默认色。
- `NotificationSlotQueue`：用 `SortedSet<int>` 管理槽位，`TryAcquire` 取**最小**可用槽（满了返回 `null`），`Release` 归还。
- `AppConfigStore.DefaultPath()` = `%APPDATA%/win-ntf/config.json`。`LoadOrCreate` 不存在则写默认；存在则反序列化 + `Validate()`。

## 约定

- 领域类型用 `record`（如 `NormalizedNotification`），small sealed 类。
- 校验失败统一抛 `NotificationValidationException`，由 `Server` 翻译成 `400`。

## 反模式

- 禁止 `using System.Windows` 或任何 WPF/ASP.NET 引用——破坏可测性与分层。
