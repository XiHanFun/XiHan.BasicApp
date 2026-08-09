# 实时通信

前端用 SignalR 接后端两条 Hub：通知 `/hubs/notification` 与聊天 `/hubs/chat`。本页讲连接怎么管、载荷有什么坑、哪些功能依赖它。

后端 Hub 侧见 [后端手册 · 即时通讯](../backend/realtime)。

## `useSignalR`

`~/composables/useSignalR.ts`，按 `hubPath` 维护**全局单例连接**——两条 Hub 各一条，互不干扰：

```ts
const { on, off, invoke, start, stop } = useSignalR('/hubs/notification')

onMounted(() => {
  on('ReceiveNotification', (payload) => { /* 处理推送 */ })
})
onUnmounted(() => {
  off('ReceiveNotification')
})
```

| API | 说明 |
| --- | --- |
| `on(method, handler)` / `off(method)` | 订阅 / 取消订阅服务端方法 |
| `invoke(method, ...args)` | 调用服务端方法 |
| `start()` / `stop()` | 手动启停 |
| `destroy()` | 清空本 Hub 的全部订阅并停止连接 |
| `destroyAllSignalRConnections()` | 登出时一把清（模块顶层导出，非 `useSignalR` 返回值） |

## 连接策略

| 环节 | 行为 |
| --- | --- |
| **认证** | 自动携带 JWT（`accessTokenFactory`）。浏览器 WebSocket 不能自定义头，SignalR 会把令牌放到查询串 `access_token` |
| **无 token 时** | **不发起连接**——避免登录页与登出后打出 401 风暴 |
| **传输回退** | WebSockets → SSE → LongPolling |
| **自动重连** | 渐进式 1s / 2s / 5s / 10s / 30s |
| **放弃重连** | token 被清除即停止（登出后不再重试） |
| **negotiate 401** | 令牌过期时借统一入口刷新后重试一次；刷新失败内部已强制登出 |

::: tip 为什么要全局单例
同一条 Hub 被多个组件订阅时若各建各的连接，服务端会为一个用户维持 N 条连接，推送也会重复触发。`useSignalR` 按 `hubPath` 复用同一条连接，组件只管订阅与取消订阅。
:::

## 载荷需应用侧手动投影

::: danger Hub 侧没有 JSON 管道的那套转换器
`long → string`、枚举 → 成员名这些转换是 **MVC JSON 管道**的配置，**不作用于 SignalR**。

后果：直接推送含 `long`（如雪花 ID）的对象，前端拿到的是会溢出精度的 Number。

约定：
- **服务端推送前手动投影**成前端契约（ID 转字符串、枚举转成员名）；
- **Hub 方法参数用 `string` 接收 ID**，服务端再自行解析。
:::

## 依赖实时通信的功能

| 功能 | Hub | 说明 |
| --- | --- | --- |
| 站内通知推送 | `/hubs/notification` | 新通知即时到达，无需轮询 |
| 强制下线 | `/hubs/notification` | 管理员踢人、删除用户时 `ForceLogout` |
| **用户偏好多端同步** | `/hubs/notification` | 其它设备保存列设置/搜索设置后推 `UserSettingChanged`，已打开的页面即时应用 |
| 在线聊天 | `/hubs/chat` | 单聊 / 群聊 / 部门群 / AI 助手会话 |
| 导出任务进度 | `/hubs/notification` | 长任务进度反馈 |

## 排查

| 现象 | 原因 |
| --- | --- |
| 连接一直 401 | 令牌无效：本地无 token 时 `start()` 直接返回不发请求，negotiate 401 只会刷新重试一次，仍失败即强制登出 |
| 收到的 ID 精度不对 | 服务端推送前没投影，`long` 直接序列化成了 Number |
| 登出后仍在重连 | 没调 `destroyAllSignalRConnections()` |
| 收到重复推送 | 组件卸载时没 `off`，或绕过 `useSignalR` 自建了连接 |
| 生产连不上但本地正常 | 反向代理没放行 WebSocket 升级（Nginx 要配 `Upgrade` / `Connection` 头） |

## 相关页面

- [后端手册 · 即时通讯](../backend/realtime)：Hub 定义、推送与在线状态
- [后端手册 · 消息通知](../backend/messaging)：通知的产生与分发
- [常用组件](./components)：消息中心 UI
