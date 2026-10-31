# 实时通信

本页说明管理端的 SignalR 连接、重连与推送处理，适用于前端开发者。

前端经 `@microsoft/signalr` 连接后端 Realtime 包的通知 Hub。在线聊天（`/hubs/chat`）随后端 Chat 包在 N3 落地，目前没有。

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

## 订阅推送

```ts
import { useSignalR } from '~/composables'

const { on, off } = useSignalR()
on('ReceiveNotification', (payload) => { /* … */ })
```

具体签名以 `packages/composables/useSignalR.ts` 为准；订阅放在模块的 `setup.ts` 或页面的生命周期里，离开时取消。

## 下一步

- [后端实时推送](../backend/realtime) · [消息通知](../backend/messaging)
