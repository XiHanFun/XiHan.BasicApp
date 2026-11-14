# 实时通信

本页说明前端的 SignalR 连接、重连与推送处理，适用于前端开发者。

前端经 `@microsoft/signalr` 连两个 Hub：后端 Realtime 包的通知 Hub（壳层负责），以及聊天模块自己连的聊天 Hub `/hubs/chat`。两条连接相互独立。

## 连接

| 项 | 说明 |
| --- | --- |
| Hub 路径 | 通知 Hub 取 `VITE_REALTIME_HUB`（仓库缺省 `/hubs/notification`），留空即不建实时连接；地址前缀取 `VITE_API_BASE_URL`（去掉末尾的 `/api`） |
| 开发代理 | Vite 把 `/hubs` 以 WebSocket 代理到后端 |
| 传输 | WebSockets，退到 Server-Sent Events、长轮询 |
| 认证 | `accessTokenFactory` 带当前访问令牌；后端只在 `/hubs` 前缀下从查询串 `access_token` 读令牌 |
| 管理 | `~/composables` 的 `useSignalR(hubPath)`：按 Hub 路径各持一条全局单例连接，订阅与重连互不干扰 |

### 重连与令牌

- 没有令牌时不发起连接；登录后连接，令牌清除即断开并销毁监听；
- 断线后自动重连：1 秒、2 秒、5 秒、10 秒、之后每 30 秒；令牌已清除（已登出）即放弃；
- 自动重连之外，断开期间每 15 秒再尝试一次建连；
- negotiate 返回 401（令牌过期）时借道 auth store 的统一刷新重试一次；刷新失败即清会话，重连随之停止；
- 重新建连后重新绑定已注册的事件监听；
- 持续断开超过 12 秒才在灵动岛提示断开（自动重连期间的瞬断不提示），恢复后提示已恢复。

## 推送方法

方法名来自生成的 `~/contracts`（`realtime.ts` 的 `RealtimeMethods`，由后端 `contracts export` 导出的 `realtime.json` 生成），不手写字符串。通知 Hub 的推送由布局里的 `useSignalRIntegration` 处理：

| 方法 | 前端处理 |
| --- | --- |
| `ForceLogout` | 会话被吊销：载荷带 `targetSessionIds` 时只处理本会话；断开连接、清会话，弹窗提示原因，确认后退出 |
| `ReceiveNotification` | 右下角通知卡片（纯文本摘要，5 秒）、提示音，并通知顶栏的通知中心刷新 |
| `TaskProgress` | 导出等后台任务进度，显示在[灵动岛](./dynamic-island)（`applyServerTaskProgress`） |
| `UserSettingChanged` | 同一用户在其他设备上改了设置：偏好与收藏按 `settingKey` 实时应用；页面设置（`scene = Page`：列设置、搜索设置、个人视图、看板布局）按页面码分发给已打开的页面，关着对应同步开关的部分不应用；载荷带 `sourceClientId`，本端据此忽略自己的回显 |

推送不保证送达：断线期间错过的通知以收件箱为准（打开通知中心或刷新时重新拉取）。强制阅读与登录弹窗的公告由布局在挂载时经 `inbox.mandatory`、`inbox.popups` 拉取，不依赖推送。

## 聊天

聊天模块（`src/modules/chat`）经壳层扩展在布局里建立 `useSignalR(RealtimeHubs.Chat)`，持有会话读权限（`chat.chat-conversations.read`）才连：

| 方向 | 方法 | 前端处理 |
| --- | --- | --- |
| 推送 | `ReceiveChatMessage`、`ChatMessageRecalled`、`ChatMessageEdited`、`ChatReactionChanged` | 按会话内序号 `seq` 并入消息流，重复到达的不覆盖已有那条；别人发来的消息不在当前会话时响提示音，不在聊天页时上灵动岛 |
| 推送 | `ChatReadPositionChanged` | 已读位只前移，群消息的已读人数按序号比 |
| 推送 | `ChatConversationChanged` | 按变更类型刷新会话列表、成员与群信息 |
| 推送 | `ChatTyping` | 显示「正在输入」4 秒 |
| 推送 | `ChatAssistantDelta`、`ChatAssistantCompleted` | 助手流式回复：增量立即推给本人，结束或失败时收到完成，失败带 `errorCode` 与 `error` |
| 调用 | `JoinConversation` / `LeaveConversation` | 打开与离开会话时进出会话组 |
| 调用 | `Typing` | 输入时节流，3 秒至多调用一次 |

客户端可调的方法名来自生成的 `RealtimeInvocations`。会话列表与消息里的时间按应用时区显示，「今天 / 昨天」也按应用时区的日历日算。附件与群头像经 `chat-messages.presign` 凭会话换签名地址，不走通用文件的签名。

## 订阅推送

```ts
import { useSignalR } from '~/composables'
import { RealtimeHubs, RealtimeMethods } from '~/contracts'

const signalR = useSignalR(RealtimeHubs.Notification)
const handler = (payload: unknown) => { /* … */ }
signalR.on(RealtimeMethods.ReceiveNotification, handler)
// 离开时
signalR.off(RealtimeMethods.ReceiveNotification, handler)
```

`useSignalR` 返回 `connection`、`connected`、`start`、`stop`、`destroy`、`on`、`off`、`invoke`；签名以 `packages/composables/useSignalR.ts` 为准。连接由布局与模块的集成钩子负责建立，页面只订阅、并在离开时取消；`invoke` 在未连接时抛错，调用方自行处理。

## 下一步

- [后端实时推送](../backend/realtime) · [在线聊天](../backend/chat) · [消息通知](../backend/messaging)
