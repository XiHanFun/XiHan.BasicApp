# 实时推送

::: info 本页范围
通知 Hub（强制下线、站内通知、任务进度、跨端设置同步）与在线状态**已落地**（Realtime 包）。**在线聊天**——`/hubs/chat`、会话、群聊、AI 助手会话——属于 Chat 包，**N3 规划中**，当前不可用。
:::

框架 5 不再包装 SignalR，实时能力是 BasicApp 自己的 Realtime 包（Tier 2）：直接用 ASP.NET Core SignalR，其他包只经 `Contracts` 的接口使用它。

## Hub

| 项 | 值 |
| --- | --- |
| 地址 | `/hubs/notification` |
| 认证 | 绕过内核管道（`BypassXiHanPipeline`）；用 ASP.NET Core 认证的 XiHan 方案，与 `Security.Jwt` 同一套参数（密钥、签发者、受众、容差、不映射入站声明） |
| 令牌 | 只在 `/hubs` 下从查询串 `access_token` 读 |
| 会话门禁 | `SessionAuthenticationFilter` 同样核对会话有效，吊销的会话连不上 |

连接建立后按**会话、用户、用户在当前一侧、当前一侧**加入各组；推送按组发出。

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
await push.SendAsync(RealtimeAudience.UsersIn(side, userIds), RealtimeMethods.ReceiveNotification, payload, jsonTypeInfo, cancellationToken);
```

| 受众 | 说明 |
| --- | --- |
| `Sessions(ids)` | 指定会话（访问令牌的 `sid`）上的连接 |
| `UsersIn(side, ids)` | 用户在某一侧的连接：同一个人切到别的租户时收不到 |
| `Users(ids)` | 用户的全部连接，不分侧 |
| `Side(side)` | 平台或某个租户的全部在线连接 |

**提交后才发**：在调用管道里（操作、按租户执行的后台调用）先暂存，`RealtimeDispatchMiddleware` 在调用成功且事务提交之后才发出，失败或回滚即丢弃；管道外的后台循环立即发出。推送**不保证送达**：接收者此刻没有连接即略过，持久的状态（收件箱、任务进度）由各包自己的表承担。

吊销会话时先推 `ForceLogout`，再把连接移出各组、宽限 2 秒后断开。

## 在线状态

`Contracts` 的在线状态接口（Realtime 实现）回答「哪些会话此刻持有实时连接」「某一侧有多少在线用户」：

- 在线用户页（`sessions.list`）标出实时在线的会话；
- 工作台的在线设备、访问概况小组件。

## 单实例限制

连接登记与组都在**进程内**：多实例部署时在线状态只看本实例，推送到不了别的实例上的连接。框架没有提供 SignalR 背板与跨实例的连接登记，方案决定暂不做；6.0 预览建议单实例部署。

## 在线聊天（规划中 · N3）

方案中的 Chat 包：会话、成员、消息、反应；`/hubs/chat`；敏感词；保留清理作业；聊天审计（`chat-audit`）；补齐 5.x 缺少的消息转发（`chat-messages.forward`）；AI 助手会话复用 Chat，助手回复流式推送（随 N4）。

## 下一步

- [消息通知](./messaging) · [前端实时通信](../frontend/realtime) · [统一认证](./authentication)
