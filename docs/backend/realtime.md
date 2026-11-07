# 实时推送

本页说明 Hub 的登记、推送方法、推送契约、在线状态与跨实例，适用于后端开发者。

::: info 本页范围
通知 Hub（强制下线、站内通知、任务进度、跨端设置同步）、在线状态与配了 Redis 时的跨实例由 Realtime 包提供；聊天 Hub `/hubs/chat` 由 Chat 包登记，会话、消息与聊天专用的推送方法见[在线聊天](./chat)。
:::

实时能力由 Realtime 包（Tier 2）提供：直接用 ASP.NET Core SignalR，其他包只经 `Contracts` 的接口（`IRealtimePush`、`IRealtimePresence`）使用它。没装 Realtime 即没有推送，各包照常工作。

## Hub

Realtime 自己登记通知 Hub，别的包的 Hub 继承 `RealtimeHub` 并在自己的 Feature 里 `AddRealtimeHub<THub>(路径)` 登记，宿主的 `MapRealtimeHubs()` 把登记过的 Hub 一并映射（包没装配即不映射）。路径必须在 `/hubs` 下，同一路径或类型重复登记即启动失败。

| 项 | 值 |
| --- | --- |
| 地址 | `/hubs/notification`；各包登记的 Hub（如 Chat 的 `/hubs/chat`） |
| 认证 | 绕过内核管道（`BypassXiHanPipeline`）；用 ASP.NET Core 认证的 XiHan 方案，与 `Security.Jwt` 同一套参数（密钥、签发者、受众、容差、不映射入站声明） |
| 令牌 | 只在 `/hubs` 下从查询串 `access_token` 读 |
| 会话门禁 | `SessionAuthenticationFilter` 同样核对会话有效，吊销的会话连不上 |

连接建立后按会话、用户、用户在当前一侧、当前一侧加入各组，再登记连接（在线状态显示为在线时一定已经收得到推送）；推送按组发出。派生的 Hub 要让连接另外加入组时用 `JoinGroupAsync`，吊销会话时这些组一并移出。

## 推送方法

| 方法 | 谁发 | 内容 |
| --- | --- | --- |
| `ForceLogout` | Identity | 会话被吊销（强制下线、改密、锁定、停用、删除），带给用户看的原因 |
| `ReceiveNotification` | Notification | 发布或提醒时按接收人推送 |
| `TaskProgress` | Files、Notification | 导出任务与通知发布的进度，只推给发起人，见[任务进度](#任务进度) |
| `UserSettingChanged` | Identity | 个人设置在其他设备上改了（偏好、收藏、看板布局跨端同步） |
| `ReceiveChatMessage` 等 | Chat | 聊天 Hub 上的消息、撤回、编辑、回应、已读位、会话变更与正在输入，见[在线聊天](./chat#推送) |

方法清单由 `contracts export` 一并导出为 `frontend/contracts/realtime.json`，前端生成 `packages/contracts/realtime.ts`。推送载荷是各包自己的记录，按各包的源生成序列化上下文序列化（枚举按名称）。

## 推送契约

```csharp
// Contracts：IRealtimePush
await push.SendAsync(RealtimeHubs.Notification, RealtimeAudience.UsersIn(side, userIds), RealtimeMethods.ReceiveNotification, payload, jsonTypeInfo, cancellationToken);
```

| 受众 | 说明 |
| --- | --- |
| `Sessions(ids)` | 指定会话（访问令牌的 `sid`）上的连接 |
| `UsersIn(side, ids)` | 用户在某一侧的连接：同一个人切到别的租户时收不到 |
| `Users(ids)` | 用户的全部连接，不分侧 |
| `Side(side)` | 平台或某个租户的全部在线连接 |

第一个参数是 Hub 路径，没登记的 Hub 在调用里即抛出。

提交后才发：在调用管道里（操作、按租户执行的后台调用）先暂存，`RealtimeDispatchMiddleware` 在调用成功且事务提交之后才发出，失败或回滚即丢弃；管道外的后台循环立即发出。推送不保证送达：接收者此刻没有连接即略过，持久的状态（收件箱、任务进度）由各包自己的表承担。

立即推送 `SendNowAsync` 给不落库的瞬时事件（正在输入、助手的流式增量）：不等提交、不随回滚撤回。操作都有工作单元，但事务到第一次写入才开：写入之前可以立即推送，事务开了之后调用即抛出 `InvalidOperationException`。

吊销会话时先推 `ForceLogout`，再经 `DisconnectAsync`（发出时机同 `SendAsync`）把连接移出全部组、宽限 2 秒后断开，任一 Hub 上的连接都算。

## 任务进度

`TaskProgress` 的载荷是 Contracts 的 `TaskProgressPayload`，推给发起人在任务那一侧的连接（`UsersIn(side, [发起人])`，切到别的租户收不到），前端灵动岛按 `taskId` 复用同一条，进行中常驻，成功或失败后自动收起。

| 字段 | 说明 |
| --- | --- |
| `taskId` | 任务标识：导出是 `export:<任务 ID>`，通知发布是 `notification-publish:<trace-id>` |
| `label` / `detail` | 文案与副文本，服务端按发起人的区域翻好 |
| `state` | `loading`、`success` 或 `error` |
| `progress` | 进度 0 到 100，为空是不确定态 |
| `link` | 点击跳转的路由（导出中心、通知公告） |

| 任务 | 什么时候推 | 怎么发出 |
| --- | --- | --- |
| 导出 | 领到任务（准备中，进度 0）；每写一千行或到 99%（已写行数）；完成（总行数，点击去导出中心）；失败（失败原因） | 随写任务表的那次调用提交后发出，失败走独立的短事务后立即发出。文案按提交时 `Accept-Language` 协商出的区域 |
| 通知发布（`notifications.publish`） | 开始（不确定态）；成功（接收人数，点击去通知公告）；失败（失败原因，异常时没有原因） | 成功随提交发出。开始与失败不随事务生效，由 Transaction 阶段开头、工作单元之外的 `PublishProgressMiddleware` 立即发出，回滚、提交失败与异常都会收尾 |

建好即发布的 `notifications.create` 不推进度。

## Hub 参数

保活、超时、消息大小等参数在配置节 `BasicApp:Realtime:SignalR`，对全部 Hub 生效，见[配置参考](../configuration#basicapp-realtime-signalr)。

## 在线状态

`Contracts` 的在线状态接口（Realtime 实现）回答「哪些会话此刻持有实时连接」「某一侧有多少在线用户」：

- 在线用户页（`sessions.list`）标出实时在线的会话；
- 工作台的在线设备、访问概况小组件。

任一 Hub 上的连接都算在线。

## 跨实例

宿主配了 Redis（绑定 `cache`）即以跨实例装配 Realtime，没配全部在本实例内：

| 方面 | 不配 Redis | 配了 Redis |
| --- | --- | --- |
| 推送与组 | 进程内 | SignalR 接 Redis 背板：一个实例发出的推送送到各实例上的连接；背板用自己的连接，配置与账号取宿主的 Redis 缓存配置，频道带同一键前缀（`XiHan:Caching:Redis:KeyPrefix` + `realtime:`） |
| 在线状态 | 本实例的连接登记 | 每个实例把自己的连接快照写进共享缓存（`realtime:presence:node:<实例>`），连接变化即重新写入，每 15 秒心跳续期、45 秒没续即过期；实例名册 `realtime:presence:nodes` 记各实例。读在线状态时汇总名册里未过期实例的快照 |
| 强制下线 | 移出组后断开本实例的连接 | 本实例的连接直接断开；别的实例上的连接经背板移出全部组（之后的推送不再送到），再以广播订阅 `basicapp.realtime.disconnect` 通知各实例断开自己的连接 |

广播要经消息代理：只配了 Redis、没配 RabbitMQ 时广播只到本实例，别的实例上被吊销的连接只是被移出全部组、不再收到推送，等客户端收到 `ForceLogout` 自行断开或令牌过期——这是写明的语义，不是兜底。写共享缓存失败只记 Warning，在线状态等下一次心跳修复，不影响连接本身。

## 下一步

- [在线聊天](./chat) · [消息通知](./messaging) · [前端实时通信](../frontend/realtime) · [统一认证](./authentication)
