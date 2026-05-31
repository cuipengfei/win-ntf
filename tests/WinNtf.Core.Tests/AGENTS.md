# WinNtf.Core.Tests

自定义可执行测试运行器，**非 xUnit/NUnit**。覆盖 `Core` 与 `Server` 行为。

## 关键事实（改动前先读）

- **测试必须手动注册**：在 [Program.cs](tests/WinNtf.Core.Tests/Program.cs) 的 `tests` 数组里加一行 `(nameof(...), ...)`，否则不会跑。
- 同步测试用 `Sync(action)` 包装成 `Func<Task>`；异步测试（如 server 集成）直接传方法引用。
- 断言用自定义 [TestAssert.cs](tests/WinNtf.Core.Tests/TestAssert.cs)，不是第三方库。
- 运行器逐个 try/catch，`PASS`/`FAIL` 打到 stdout/stderr；有失败返回 exit code `1`，全过返回 `0`。
- `LocalHttpServerTests` 起真实轻量 loopback server 做集成测试（loopback host 校验、kebab-case position、超大 payload 413 等）。

## 约定

- 测试名描述行为，如 `NotifyAcceptsKebabCasePosition`、`NormalizeRejectsEmptyText`。
- 优先覆盖 normalization / validation / queueing / positioning / server 响应，再做手动视觉测试。

## 运行

```bash
dotnet run --project tests/WinNtf.Core.Tests/WinNtf.Core.Tests.csproj --configuration Release
```
