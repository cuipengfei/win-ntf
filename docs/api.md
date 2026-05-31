# HTTP API 草案

## `GET /health`

返回：

```text
ok
```

成功状态码：`200`

## `POST /notify`

请求头：

```text
Content-Type: application/json
```

请求体：

```json
{
  "title": "✅ project",
  "text": "Task completed",
  "variant": "success",
  "color": "#4ADE80",
  "durationMs": 10000,
  "position": "top-right",
  "persistent": false
}
```

字段：

- `text`: 必填，空白文本返回 `400`。
- `title`: 可选，默认 `win-ntf`。
- `variant`: 可选，`info | success | warning | error | tool`。
- `color`: 可选，覆盖 variant 默认色。
- `durationMs`: 可选，默认 `10000`；`0` 表示不自动关闭。
- `position`: 可选，`center | top-right | bottom-right`。
- `persistent`: 可选，`true` 时不自动关闭。

成功状态码：`202`

错误：

- `400`: JSON 无效、`text` 为空、字段非法。
- `413`: payload 超过限制。
