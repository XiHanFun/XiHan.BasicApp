# 实时推送

本页说明 Hub 的登记、推送方法、推送契约、在线状态与跨实例，适用于后端开发者。

::: info 本页范围
通知 Hub（强制下线、站内通知、任务进度、跨端设置同步）、在线状态与配了 Redis 时的跨实例已落地（Realtime 包）。在线聊天（`/hubs/chat`、会话、群聊、AI 助手会话）属于 Chat 包，N3 规划中，当前不可用。
:::

框架 5 不再包装 SignalR，实时能力是 BasicApp 自己的 Realtime 包（Tier 2）：直接用 ASP.NET Core SignalR，其他包只经 `Contracts` 的接口使用它。

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
| `TaskProgress` | Files | 导出任务进度 |
| `UserSettingChanged` | Identity | 个人设置在其他设备上改了（偏好、收藏、看板布局跨端同步） |

方法清单由 `contracts export` 一并导出为 `frontend-next/contracts/realtime.json`，前端生成 `packages/contracts/realtime.ts`。推送载荷是各包自己的记录，按各包的源生成序列化上下文序列化（枚举按名称）。

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

立即推送 `SendNowAsync` 给不落库的瞬时事件（正在输入、N4 的助手流式增量）：不等提交、不随回滚撤回，在开着事务的调用里调用即抛出 `InvalidOperationException`。

吊销会话时先推 `ForceLogout`，再把连接移出全部组、宽限 2 秒后断开，任一 Hub 上的连接都算。

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
| 在线状态 | 本实例的连接登记 | 每个实例把自己的连接快照写进共享缓存（`realtime:presence:node:<实例>`），连接变化即重写，每 15 秒心跳续期、45 秒没续即过期；实例名册 `realtime:presence:nodes` 记各实例。读在线状态时汇总名册里未过期实例的快照 |
| 强制下线 | 移出组后断开本实例的连接 | 本实例的连接直接断开；别的实例上的连接经背板移出全部组（之后的推送不再送到），再以广播订阅 `basicapp.realtime.disconnect` 通知各实例断开自己的连接 |

广播要经消息代理：只配了 Redis、没配 RabbitMQ 时广播只到本实例，别的实例上被吊销的连接只是被移出全部组、不再收到推送，等客户端收到 `ForceLogout` 自行断开或令牌过期——这是写明的语义，不是兜底。写共享缓存失败只记 Warning，在线状态等下一次心跳修复，不影响连接本身。

## 在线聊天（规划中 · N3）

方案中的 Chat 包：会话、成员、消息、反应；`/hubs/chat`；敏感词；保留清理作业；聊天审计（`chat-audit`）；补齐 5.x 缺少的消息转发（`chat-messages.forward`）；AI 助手会话复用 Chat，助手回复流式推送（随 N4）。

## 下一步

- [消息通知](./messaging) · [前端实时通信](../frontend/realtime) · [统一认证](./authentication)
