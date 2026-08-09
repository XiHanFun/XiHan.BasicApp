# 缓存与异步

BasicApp 的热点读路径走 Redis 分布式缓存，写路径**精准失效**；耗时动作（发邮件短信、导出）不占请求线程，走队列 + 后台消费。这两条机制的时序稍有偏差就会出现「改完不生效」「读到脏数据」「任务丢了」，本页把它们说透。

## 缓存

### 三层结构

```text
① 缓存条目类（Application/Caching/Saas*CacheItem.cs）
     一个热点读定义一个条目类，用 [CacheName] 绑定缓存名并定义缓存值结构
② 缓存键常量（SaasCacheNames / SaasCacheKeys）
     缓存名一律 const 集中在 SaasCacheNames，业务键/匹配模式由 SaasCacheKeys 拼，禁止内联字符串
③ 失效器（ISaasCacheInvalidator / SaasCacheInvalidator）
     按域提供精准失效方法，底层 RemoveByPatternAsync(..., considerUow: true)
```

### 缓存条目一览

| 缓存条目 | 键前缀 | 用途 |
| --- | --- | --- |
| `SaasAuthorizationSnapshotCacheItem` | `basicapp:saas:auth:snapshot` | **授权快照**——请求期鉴权热路径，让授权变更免重登即生效 |
| `SaasMenuRoutesCacheItem` | `basicapp:saas:navigation:routes` | 菜单路由（前端动态路由数据源） |
| `SaasConfigValueCacheItem` | `basicapp:saas:config:value` | 系统参数值 |
| `SaasDictItemTreeCacheItem` | `basicapp:saas:configuration:dict-tree` | 字典项树（下拉高频读） |
| `SaasEditionGateCacheItem` | `basicapp:saas:tenancy:edition-gate` | 租户版本权限白名单门控 |
| `SaasEnabledEditionsCacheItem` | `basicapp:saas:tenancy:editions` | 启用中的版本套餐 |
| `SaasMessageTemplateCacheItem` | `basicapp:saas:message:template` | 消息模板（发送链路高频读） |
| `SaasDepartmentTreeCacheItem` | `basicapp:saas:organization:dept-tree` | 部门树 |
| `SaasUserSettingCacheItem` | `basicapp:saas:user:setting` | 用户 UI 偏好 |
| `SaasSessionStateCacheItem` | `basicapp:saas:identity:session-state` | 会话状态（会话闸门读它判 401/423） |
| `SaasPermissionSelectCacheItem` / `SaasRoleSelectCacheItem` / `SaasResourceSelectCacheItem` / `SaasOperationSelectCacheItem` | `basicapp:saas:{域}:select` | 各类下拉选择项 |
| `SaasTelegramConversationStateCacheItem` | `basicapp:saas:bot:telegram-conversation` | Telegram 会话态 |

缓存名统一在 `SaasCacheNames` 里定义为 `const`（条目类用 `[CacheName]` 引用），业务键与匹配模式拼接在 `SaasCacheKeys`。**新增缓存必须走这套，不要内联字符串**——否则失效器找不到你的键。

### 失效器

`ISaasCacheInvalidator` 按域给出精准失效方法，而不是「一把清空」：

| 方法 | 影响 |
| --- | --- |
| `InvalidateAuthorizationAsync(userId?)` | 授权快照；传 `userId` 只清该用户，不传清全部 |
| `InvalidateNavigationAsync()` | 菜单路由 |
| `InvalidateConfigurationAsync(configKey?)` | 系统参数 |
| `InvalidateDictionaryAsync()` | 字典项树 |
| `InvalidateEditionGateAsync()` / `InvalidateTenantEditionAsync()` | 版本门控 / 版本套餐 |
| `InvalidateOrganizationAsync()` | 部门树 |
| `InvalidateMessageTemplateAsync()` | 消息模板 |
| `InvalidateUserSettingAsync(userId)` | 指定用户偏好 |
| `InvalidatePermissionDefinitionAsync()` / `InvalidateRoleDefinitionAsync()` / `InvalidateResourceDefinitionAsync()` / `InvalidateOperationDefinitionAsync()` | 各类定义与下拉 |
| `InvalidateSessionStateAsync(userSessionId)` / `InvalidateAllSessionStatesAsync()` | 会话状态 |

写侧 `*AppService` 改完数据后调对应方法，例如 `ConfigAppService` 在增删改后调 `InvalidateConfigurationAsync`。

### `considerUow: true` 是关键

失效底层一律是：

```csharp
_configValueCache.RemoveByPatternAsync(pattern, hideErrors: true, considerUow: true, token: ct);
```

`considerUow: true` 表示**排队到工作单元提交之后**才真正清缓存。

::: danger 为什么不能立即清
如果在事务提交前就清了缓存，并发请求会立刻回源查库，读到的是**尚未提交的旧值**，然后把旧值重新写回缓存——事务提交后缓存反而是错的，且不会自愈。这类问题极难复现，务必保持 `considerUow: true`。
:::

### 加一个新缓存的清单

1. 在 `SaasCacheNames` 加缓存名常量；必要时在 `SaasCacheKeys` 加业务键/匹配模式拼接方法。
2. 新建 `SaasXxxCacheItem`，用 `[CacheName]` 绑定第 1 步的常量并定义值结构。
3. 读侧 `*QueryService` 里先查缓存、未命中回源并回填（过期时间在回填时用 `DistributedCacheEntryOptions` 指定）。
4. 在 `ISaasCacheInvalidator` / `SaasCacheInvalidator` 加一个 `InvalidateXxxAsync`，底层用 `RemoveByPatternAsync(..., considerUow: true)`。
5. **所有会改到该数据的写侧方法都要调它**——漏一个就会出现「某个入口改完不生效」。

## 异步：队列承载工作

原则是**队列承载工作 + 拉取消费**，不在请求线程里做耗时动作，也不额外造心跳/调度层。

```text
写侧（请求线程）                      后台常驻服务（XiHanBackgroundServiceBase）
   │ 业务行落库（状态 Pending）
   │ 事务提交
   ▼
IRedisDelayQueue<T>.EnqueueAsync ─────►  拉取 → 原子领取（置 Sending / Processing）→ 执行 → 置终态
   （提交之后才入队）                       │
                                            └─ 启动时复位崩溃残留的在途状态 → Pending 并重投
```

三条设计要点：

1. **数据库表是事实源，队列只承载「待办工作」**。状态、重试次数、定时、审计都在表里；队列里只有一个轻量消息（通常就是实体 Id）。
2. **提交之后才入队**，保证后台拉到消息时业务行已经可见（无环境工作单元时直接入队）。
3. **原子领取**：发件箱的 `TryClaimForSendingAsync` 原子地把行由 `Pending` / 可重试 `Failed` 置为 `Sending`（天然去重、按 `MaxRetryCount` 自限重试）；导出的 `ClaimByIdAsync` 原子地把行由 `Pending` 置为 `Processing`。

### 现有的两条异步链路

| 链路 | 入队方 | 消费方 | 事实源表 |
| --- | --- | --- | --- |
| **邮件 / 短信发件箱** | `DbMessageOutbox.EnqueueAsync(channel, entityId)` | `MessageOutboxHostedService` | `Sys_Email` / `Sys_Sms` |
| **异步导出** | `ExportTaskAppService` 提交时入队 | `ExportTaskHostedService` | `Sys_Export_Task` |

两者都继承框架的 `XiHanBackgroundServiceBase<T>`（封装了循环拉取、并发控制、重试、优雅停机、运行统计），子类实现 `FetchWorkItemsAsync`（取任务）和 `ProcessItemAsync`（处理任务）两个抽象方法，并重写 `ExecuteAsync` 在循环前做一次启动复位。

### 崩溃恢复

进程被杀时可能留下一批处于在途状态（发件箱 `Sending`、导出 `Processing`）的行——它们既不会被再次领取，也永远不会完成。所以后台服务**启动时先做一次复位**：发件箱用 `ResetInFlightAndCollectPendingAsync` 把残留的 `Sending` 改回 `Pending` 并收集待发送重新投递；导出用 `ResetOrphanedProcessingAsync` 复位后再由 `GetPendingIdsAsync` 取出待执行任务重投。

新增异步链路时这一步别漏，否则一次异常重启就会永久卡住一批任务。

### 与框架后台作业的分工

| 需求 | 用什么 |
| --- | --- |
| 周期性/定时触发（每天凌晨跑报表） | 框架 [Tasks](https://framework.docs.xihanfun.com/packages/tasks) 的调度器 + `Sys_Task` 配置 |
| 一次性 fire-and-forget，入队即返回 | 框架 `IBackgroundJobManager.EnqueueAsync` |
| **持续拉队列消费，且要业务表做状态机** | **`XiHanBackgroundServiceBase<T>` + `IRedisDelayQueue<T>`（BasicApp 的两条链路都是这种）** |
| 等人审批的长流程 | [工作流](./workflow) |

## 事件

BasicApp 的写路径大量使用本地事件解耦（如授权变更 → 写权限变更日志、待办创建 → 发站内通知）。

::: warning 事件处理器必须显式登记
本地事件总线只订阅登记在 `XiHanLocalEventBusOptions.Handlers` 里的处理器类型。裸 `services.AddTransient<MyHandler>()` **不会被订阅，静默失败**。必须用 `AddSaasLocalEventHandler<T>()`（内部 `AddTransient` + 把类型加进 `XiHanLocalEventBusOptions.Handlers`），并在 `AddSaasEventHandlers` 里登记。
:::

发布时机（框架保证）：

- **本地事件在事务提交前**发布——处理器可能继续写库，这些写入必须落在同一事务里。
- **分布式事件在事务提交成功后**发布——避免「事务回滚了事件照发」的幽灵事件。

## 排查速查

| 现象 | 原因 |
| --- | --- |
| 改了配置/菜单/字典，页面还是旧值 | 写侧漏调对应的 `InvalidateXxxAsync` |
| 偶发读到旧值且不自愈 | 失效没走 `considerUow: true`，在提交前清了缓存 |
| 授权改了不生效 | `InvalidateAuthorizationAsync` 没调；注意传 `userId` 只清单个用户 |
| 邮件/短信卡在 `Sending` 不动 | 进程崩溃残留，检查后台服务的启动复位逻辑是否执行 |
| 导出任务提交后没反应 | 队列是否入队（是否在提交后）、后台服务是否在跑 |
| 事件处理器不触发 | 没加进 `XiHanLocalEventBusOptions.Handlers` |

## 相关页面

- [请求生命周期](./request-lifecycle)：收尾顺序的完整时序
- [数据模型](./data-model)：事实源表的结构
- [消息中心](./messaging)：邮件/短信/通知的业务设计
- [Caching 包](https://framework.docs.xihanfun.com/packages/caching) / [Tasks 包](https://framework.docs.xihanfun.com/packages/tasks)：框架侧实现
