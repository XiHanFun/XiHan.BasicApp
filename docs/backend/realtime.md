# 即时通讯

后端的 SignalR 侧：两条 Hub、怎么往前端推、在线状态怎么算，以及一条必须遵守的载荷约定。

前端接法见 [前端手册 · 实时通信](../frontend/realtime)。

## 两条 Hub

| Hub | 路径 | 用途 |
| --- | --- | --- |
| `BasicAppNotificationHub` | `/hubs/notification` | 通知推送、强制下线、偏好多端同步、任务进度 |
| `BasicAppChatHub` | `/hubs/chat` | 在线聊天（单聊 / 群聊 / 部门会话） |

两条 Hub 都继承框架的 `XiHanHub`、打 `[AuthorizeHub]`（**必须携带有效 JWT 才能连接**），在 Saas 模块的 `OnApplicationInitialization` 里映射。

Hub 路由前缀由配置 `XiHan:Web:Api:Auth:SignalRHubPathPrefix` 决定，默认 `/hubs`。

## 往前端推：`IRealtimeNotificationService<THub>`

业务侧不直接操作 Hub，而是注入按 Hub 泛型化的推送服务：

```csharp
public class NotificationAppService(
    IRealtimeNotificationService<BasicAppNotificationHub> realtime) : SaasApplicationService
{
    public async Task PublishAsync(NotificationCreateDto input, CancellationToken ct = default)
    {
        // …落库…
        // 按 userId 点发给指定用户
        await realtime.SendToUsersAsync(targetUserIds, "ReceiveNotification", payload, ct);
    }
}
```

泛型参数决定推到哪条 Hub，编译期就不会推错。

当前在用的地方：

| 场景 | 应用服务 | 说明 |
| --- | --- | --- |
| 通知推送 | `NotificationAppService` | 新公告即时到达 |
| **强制下线** | `UserAppService` | 删除用户、踢下线时 `ForceLogout` |
| **偏好多端同步** | `UserSettingAppService` | 保存列设置/搜索设置后推 `UserSettingChanged` |
| 聊天消息 | 聊天相关服务 | 落库后按会话成员 `userId` 点发 |

## 聊天的实时方法名

方法名集中在 `ChatRealtimeMethods` 常量类，**不要内联字符串**：

| 常量 | 触发时机 |
| --- | --- |
| `ReceiveChatMessage` | 收到新消息 |
| `ChatMessageEdited` | 消息被编辑 |
| `ChatMessageRecalled` | 消息被撤回 |
| `ChatConversationChanged` | 会话变更（成员、名称等） |
| `ChatReactionChanged` | 表情回应变更 |
| `ChatReadPositionChanged` | 已读位置变更 |
| `ChatTyping` | **对端输入中**——Hub 组内广播，**不落库** |
| `ChatAssistantDelta` | AI 助手回复增量 |
| `ChatAssistantCompleted` | AI 助手回复结束 |

### 聊天 Hub 的客户端可调方法

| 方法 | 说明 |
| --- | --- |
| `JoinConversation(conversationId)` | 加入会话组 |
| `LeaveConversation(conversationId)` | 离开会话组 |
| `Typing(conversationId)` | 广播「正在输入」 |

**参数是 `string`**——这不是随意选的，见下面的载荷约定。

::: tip 消息落库后点发，而不是组内广播
聊天消息先落库，再经 `IRealtimeNotificationService<BasicAppChatHub>` **按成员 `userId` 点发**。组（group）只用于 `Typing` 这种不落库的瞬时信号。

这样做的好处：离线成员下次拉历史能看到，不依赖当时是否在线。
:::

## 在线状态怎么算

```text
在线用户列表 = 活跃会话（数据库 SysUserSession）
              + 实时连接标注（框架 IConnectionManager）
```

`BasicAppNotificationHub` 在连接 / 断开时除了框架级的连接登记外，**同步刷新对应 `SysUserSession` 的最后活动时间**——弥补 HTTP 侧无心跳的缺口。

::: tip 会话刷新失败不阻断连接
刷新失败只记日志，绝不影响连接建立或断开。实时能力不应该因为一次数据库抖动就不可用。
:::

「在线用户」页（`/identity/online-user`）读的就是这两者的合并结果。

## 载荷必须手动投影

::: danger Hub 侧没有 MVC 的 JSON 转换器
`long → string`、枚举 → 成员名这些转换是 **MVC JSON 管道**（`XiHanWebCoreMvcOptions`）的配置，**不作用于 SignalR**。

后果：直接推送含雪花 ID 的对象，前端拿到的是会溢出精度的 Number。

两条约定：
1. **服务端推送前手动投影**成前端契约——ID 转字符串、枚举转成员名；
2. **Hub 方法参数用 `string` 接收 ID**，服务端自行解析。
:::

写新推送时先问一句「这个 payload 里有 `long` 或枚举吗」，有就先投影。

## 部署注意

::: warning 反向代理要放行 WebSocket
Nginx 等反向代理必须转发 `Upgrade` / `Connection` 头，否则 WebSocket 握手失败。SignalR 会回退到 SSE / LongPolling，功能能用但实时性和资源占用都变差——**症状是「能收到消息但延迟高」，容易被误判成业务问题**。
:::

多实例部署时，SignalR 默认是单机内存的连接管理。要跨实例推送需接 backplane（Redis），否则用户连在 A 实例、消息从 B 实例发出就收不到。

## 相关页面

- [前端手册 · 实时通信](../frontend/realtime)：`useSignalR` 与连接管理
- [消息通知](./messaging)：通知的产生、模板与多通道分发
- [框架 · Web.RealTime](https://framework.docs.xihanfun.com/packages/web-realtime)：SignalR 集成的框架实现
- [配置参考](../configuration#realtime-signalr)：SignalR 配置项
