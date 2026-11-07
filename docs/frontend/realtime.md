# 实时通信

本页说明前端的 SignalR 连接、重连与推送处理，适用于前端开发者。

前端经 `@microsoft/signalr` 连接后端 Realtime 包的通知 Hub；聊天模块另连聊天 Hub `/hubs/chat`（两条独立连接，见下文「聊天」）。

## 连接

| 项 | 说明 |
| --- | --- |
| Hub 路径 | `VITE_REALTIME_HUB`，缺省 `/hubs/notification`；留空即不建实时连接 |
| 开发代理 | Vite 把 `/hubs` 以 WebSocket 代理到后端 |
| 认证 | `accessTokenFactory` 带当前访问令牌（后端只在 `/hubs` 下从查询串 `access_token` 读） |
| 管理 | `~/composables` 的 `useSignalR`：按 Hub 路径各持一条全局单例连接，订阅与重连互不干扰 |

### 重连与令牌

- 没有令牌时不发起连接；
- 断线后渐进式重连：1 秒、2 秒、5 秒、10 秒、30 秒；令牌已清除（已登出）即放弃重连；
- negotiate 返回 401（令牌过期）时借道请求层的统一刷新重试一次；刷新失败即登出，重连随之停止；
- 重连后重新绑定已注册的事件监听。

## 推送方法

方法名来自生成的 `~/contracts` `realtime.ts`（由后端 `contracts export` 导出的 `realtime.json` 生成），不手写字符串：

| 方法 | 前端处理 |
| --- | --- |
| `ForceLogout` | 会话被吊销：提示原因、清会话、回登录页 |
| `ReceiveNotification` | 顶栏通知中心刷新、提示音；横幅与弹窗按收件箱规则展示 |
| `TaskProgress` | 导出等后台任务进度，显示在灵动岛（`applyServerTaskProgress`） |
| `UserSettingChanged` | 偏好、收藏、列设置、看板布局在其他设备上改了：按场景实时应用；载荷带 `sourceClientId`，本端据此过滤自己的回显 |

推送不保证送达：断线期间错过的通知以收件箱为准（重连或打开通知中心时重新拉取）。

## 聊天

聊天模块（`src/modules/chat`）在布局里经壳层扩展建立 `useSignalR(RealtimeHubs.Chat)`，持有会话读权限（`chat.chat-conversations.read`）才连：

| 方向 | 方法 | 前端处理 |
| --- | --- | --- |
| 推送 | `ReceiveChatMessage`、`ChatMessageRecalled`、`ChatMessageEdited`、`ChatReactionChanged` | 按会话内序号 `seq` 并入消息流，重复到达的不覆盖已有那条 |
| 推送 | `ChatReadPositionChanged` | 已读位只前移，群消息的已读人数按序号比 |
| 推送 | `ChatConversationChanged` | 按变更类型刷新会话列表、成员与群信息 |
| 推送 | `ChatTyping` | 显示「正在输入」4 秒 |
| 推送 | `ChatAssistantDelta`、`ChatAssistantCompleted` | 助手流式回复：增量立即推给本人，结束或失败时收到完成，失败带 `errorCode` 与 `error` |
| 调用 | `JoinConversation` / `LeaveConversation` | 打开与离开会话时进出会话组 |
| 调用 | `Typing` | 输入时节流 3 秒调用一次 |

客户端可调的方法名同样来自生成的 `RealtimeInvocations`。会话列表与消息里的时间按偏好设置的应用时区显示（与审计页的 `formatDate` 同一个时区），「今天 / 昨天」也按应用时区的日历日算。附件与群头像经 `chat-messages.presign` 凭会话换签名地址，不走通用文件的签名。

## 订阅推送

```ts
import { useSignalR } from '~/composables'

const { on, off } = useSignalR()
on('ReceiveNotification', (payload) => { /* … */ })
```

具体签名以 `packages/composables/useSignalR.ts` 为准；订阅放在模块的 `setup.ts` 或页面的生命周期里，离开时取消。

## 下一步

- [后端实时推送](../backend/realtime) · [在线聊天](../backend/chat) · [消息通知](../backend/messaging)
