来源：https://basicapp.docs.xihanfun.com/backend/caching

# 缓存与异步

本页说明缓存接口、缓存时长与失效、缓存管理，以及定时作业、后台循环、日志队列、发件箱与提交后推送等异步机制，适用于后端开发者。

## 缓存

### 缓存接口

| 接口 | 来源 | 用途 |
| --- | --- | --- |
| `ISharedCache` | 框架 `Caching.Abstractions` | 跨实例共享的缓存，各包都用它：授权规则、角色指派、会话门禁、验证码与登录节流、套餐白名单、打印模板等 |
| `ICache` | 框架 | 类型化缓存；配了 Redis 时是本机 L1 + Redis L2 |
| `ICacheStore` | 框架 | 字节存储，供分布式锁与幂等去重 |

不配 Redis 时是 Full Profile 的进程内实现（`XiHan:Caching:InMemory`）：零外部依赖，单实例部署即可。多实例时各实例的缓存不共享，写入方的即时失效只对本实例生效，其他实例最迟按缓存时长过期。

配了 Redis（绑定 `cache` 或 `XiHan:Caching:Redis:Configuration`，见[配置参考](../configuration#分布式缓存与消息)），宿主装上 `Caching.Redis` 与 `Caching.Hybrid`：`ISharedCache`、字节存储与分布式锁走 Redis，失效对所有实例生效；`ICache` 的写入经 Redis 频道失效各实例的 L1。Redis 连不上时启动即失败。

进程内实现直接存对象引用，Redis 下值要序列化。所以缓存的值要能按 JSON 往返，取回的对象不要原地修改；Access 等包把缓存值存成 JSON 字符串，新写的缓存照此做。

### 缓存时长与失效

| 数据 | 时长 | 失效 |
| --- | --- | --- |
| 会话门禁 | `BasicApp:Auth:SessionCacheSeconds`（30 秒） | 注销、吊销、改密、锁屏、解锁、结束模仿时 |
| 套餐白名单（按租户） | 30 秒 | 换套餐、改白名单时 |
| 权限规则、角色指派、继承图、委托、权限条件 | 60 秒 | 写入方即时失效 |
| 访问策略、字段安全规则、约束规则（按侧） | 60 秒 | 写入方即时失效 |
| 打印模板（按侧 × 编码） | 30 分钟 | 模板写入时 |
| 租户目录、权限授予 | 框架进程内缓存，`XiHan:MultiTenancy:Database:CacheTimeToLiveSeconds` / `XiHan:Security:Database:CacheTimeToLiveSeconds`（30 秒） | 框架 `TenantRecordStore` / `PermissionGrantRecordStore` 写入时失效本实例的条目 |

授予与指派的生效、到期在判定时按当前时间比较，不依赖缓存重建。

上面这些读穿缓存未命中时，经 `Runtime.Persistence.StoreScope` 在独立的只读工作单元里读库，看不到调用里还没提交的写入。所以写入方在调用里失效要用 `CacheInvalidation.RemoveAsync` / `RemoveManyAsync`（Runtime）：调用里只记下，由 Transaction 阶段开头的 `CacheInvalidationMiddleware` 在工作单元提交或回滚之后才删键（回滚了照样删，多失效一次只是多读一次库；同一缓存的键一次往返）。管道之外（种子、作业）调用即刻删除。

::: warning 不要在提交前删键
直接调 `cache.RemoveAsync` 会在提交之前删键。同一请求里的后续读取或并发请求在删键与提交之间未命中，会把提交前的旧数据写回缓存，变更要等缓存过期才生效。新写的读穿缓存一律经 `CacheInvalidation` 失效。
:::

### 命名空间与缓存管理

每个包用 `AddCacheNamespace(包名, editable)` 登记键前缀 `<包名>:`。缓存管理只开放登记过的前缀，框架自己的键（如分布式锁）不出现：

| 命名空间 | 能否改写值 |
| --- | --- |
| `access:`、`identity:`、`tenancy:`、`openplatform:` | 否（鉴权、签名与套餐门控相关） |
| `ai:` | 否（提供商目录的戳键） |
| `realtime:`（配了 Redis 时才有） | 否（在线状态） |
| `notification:`、`printing:` | 是 |

不可改写的命名空间仍可删除，删除后缓存按需重建。

缓存管理在 Maintenance 包，权限码只在平台一侧：

| 操作 | 权限码 | 说明 |
| --- | --- | --- |
| `caches.namespaces` | `maintenance.caches.read` | 已登记的命名空间 |
| `caches.keys` | `maintenance.caches.read` | 按前缀列键，按续页标记（`continuation`）往下翻 |
| `caches.inspect` | `maintenance.caches.read` | 查看值、类型与剩余存活 |
| `caches.update` | `maintenance.caches.update` | 改写值，保留原到期时间；只对可改写的命名空间 |
| `caches.delete` / `caches.delete-by-prefix` | `maintenance.caches.delete` | 按键或按前缀删除 |

进程内模式下对象缓存与字节存储是两张表，`caches.keys` 两层合起来列（先对象缓存、再字节存储），每个键标出所在的层。Redis 下只有一层，列键是 SCAN：一段可能一个键都没有，`caches.keys` 在操作里接着列到攒满一页（扫空一段提示值加倍，最多 100 次往返）；同一个键可能出现在两页，同一页内去重。

## 异步与后台工作

BasicApp 没有自建的后台任务框架，异步工作分下面几类：

| 类型 | 机制 | 例子 |
| --- | --- | --- |
| 定时作业 | 框架 `Scheduling.Database`：Feature 里 `AddJob<T>(名称, 计划)` 代码登记；运行时作业经 Maintenance 的作业管理登记 | 见下表 |
| 后台循环 | `IHostedFeature` 随应用启停 | Notification 的发送循环与 Telegram 机器人；Auditing 的日志批量写入；Files 的导出执行；Realtime 的在线状态心跳（配了 Redis 时） |
| 有界队列 | 调用里只入队，后台批量落库 | 七类分表日志（`BasicApp:Auditing:QueueCapacity` / `BatchSize` / `FlushIntervalSeconds`）；操作日志走框架的 `XiHan:Auditing:Database` 队列 |
| 提交后推送 | `IRealtimePush.SendAsync` 在调用里暂存，`RealtimeDispatchMiddleware` 在调用成功、事务提交之后发出 | `ForceLogout`、`ReceiveNotification`、`TaskProgress` |
| 提交后失效 | `CacheInvalidation` 暂存，`CacheInvalidationMiddleware` 在工作单元结束后删键 | Access 的授权类缓存、会话门禁、套餐白名单 |
| 领域事件 | 发件箱随业务写入同一事务，后台投递给订阅方 | 见[实体基类](./entity#领域事件) |

不落库的瞬时推送（正在输入、助手的流式增量）用 `IRealtimePush.SendNowAsync`，只能在事务之外调用。

代码登记的作业：

| 作业 | 计划（cron，UTC） | 做什么 |
| --- | --- | --- |
| `auditing.retention` | `BasicApp:Auditing:RetentionCron`（缺省 `0 3 * * *`） | 按保留天数清理日志，见[日志审计](./logging) |
| `chat.messages.cleanup` | `0 4 * * *` | 按保留期清理聊天消息，见[在线聊天](./chat) |
| `tenancy.tenants.expire` | `*/5 * * * *` | 把已过到期时间的租户标为到期，并停用租户目录里的记录 |
| `files.uploads.expire` | `15 * * * *` | 清理过期的分片上传 |
| `files.temporary.purge` | `45 * * * *` | 清理到期的临时文件，见[文件与存储](./file#临时文件) |
| `openplatform.oauth-cleanup` | `30 3 * * *` | 物理删除过期一天的授权码与过期七天的令牌 |

作业的查看、暂停、立即执行与改计划见[定时任务](./scheduling)。

### 发件箱

- 宿主装了框架的 `OutboxFeature`：领域事件先随业务事务落表 `outbox_message`，再由后台投递到消息代理（`XiHan:Messaging:Outbox`）。没配 RabbitMQ 时是进程内消息，配了即经 RabbitMQ 投递。
- 邮件、短信与机器人消息的待发送记录是应用表（`SysEmail`、`SysSms`、`SysBotMessage`），本身就是可查询、可重发的发件箱。别的包经 Contracts 的 `IMessageSender` 排队邮件与短信（模板不存在、渲染失败或没有可用配置即失败，不写记录）。
- 发送循环按 `BasicApp:Notification:DispatchInterval` 轮询（有新记录时提前唤醒），用条件更新领取记录，多实例只有一个能领到；失败第 n 次后等 n 倍 `RetryDelay`，至多 `MaxAttempts` 次。启动时把发送中超过 `StuckAfter` 的记录改回排队中。

### 导出

导出任务（`export-tasks.submit`）进进程内队列，后台以发起人的凭据逐页调用对应的列表操作（权限、租户、数据范围与字段安全照常生效），生成 CSV / XLSX 存成 7 天的临时文件，进度经 `TaskProgress` 推送。见[文件与存储](./file#导出中心)。

## 下一步

- [定时任务](./scheduling) · [消息通知](./messaging) · [实时推送](./realtime)
