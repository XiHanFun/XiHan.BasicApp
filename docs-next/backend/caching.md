# 缓存与异步

本页说明 6.0 的缓存用法与缓存管理，以及定时作业、托管循环、日志队列与提交后推送等异步机制，适用于后端开发者。

## 缓存

### 缓存接口

| 接口 | 来源 | 用途 |
| --- | --- | --- |
| `ISharedCache` | 框架 `Caching.Abstractions` | 跨请求共享的对象缓存：授权规则、指派、会话门禁、验证码散列、登录节流计数、条件与策略、套餐白名单 |
| `ICache` / 字节存储 | 框架 | 框架内部（分布式锁、租约等） |

不配 Redis 时是 Full Profile 的进程内实现：零外部依赖，单实例部署即可；多实例时各实例的缓存不共享（授权规则写入方即时失效只对本实例生效，其他实例最迟按缓存时长过期）。配了 Redis 连接（绑定 `cache`，见[配置参考](../configuration#分布式缓存与消息)），宿主装上 `Caching.Redis` 与 `Caching.Hybrid`：`ISharedCache`、字节存储与分布式锁走 Redis，写入方的失效对所有实例生效；`ICache` 是本机 L1 + Redis L2，写入经 Redis 频道失效各实例的 L1。

Redis 下 `ISharedCache` 的值要序列化（框架的 JSON 缓存序列化器），进程内模式存的是对象引用；缓存的类型因此要能按 JSON 往返，取回的对象也不要原地修改。

### 缓存时长与失效

| 数据 | 时长 | 失效 |
| --- | --- | --- |
| 会话门禁 | `BasicApp:Auth:SessionCacheSeconds`（30 秒） | 吊销、锁屏、解锁会话时清除（提交之后） |
| 套餐白名单（按租户） | 30 秒 | 换套餐、改白名单时清除（提交之后） |
| 权限规则、角色指派（按主体） | 60 秒 | 写入方即时失效 |
| 字段安全规则（按侧 × 实体） | 60 秒 | 写入即失效 |
| 启用的约束规则（按侧） | 60 秒 | 写入即失效 |
| 租户目录、授予存储 | 框架有界 TTL | 框架 `TenantRecordStore` / `PermissionGrantRecordStore` 写入时失效 |

有效期类的判定（授予与指派的生效 / 到期）在判定时按当前时间比较，不依赖缓存重建。

失效的时机：Access 的读穿缓存（权限规则、角色指派、继承图、委托、条件、访问策略、字段安全、约束规则）、Identity 的会话门禁与 Tenancy 的套餐白名单缓存未命中时在独立的只读工作单元里读库，看不到调用里还没提交的写入；所以写入方在调用里的失效经 Runtime 的 `CacheInvalidation` 只记下，由 Transaction 阶段开头的 `CacheInvalidationMiddleware` 在工作单元提交或回滚之后才删键（回滚了照样删，多失效一次只是多读一次库；同一缓存的键一次往返）。若在提交之前删键，同一请求里的后续读取（如新建角色委托时的约束检查）或并发请求（如被强制下线的会话恰在提交前发来的请求）会把提交前的旧数据写回缓存，变更要等缓存过期才生效。新写的读穿缓存照此失效。

### 命名空间与缓存管理

每个包用 `AddCacheNamespace` 登记自己的键前缀，缓存管理只开放登记过的前缀，框架自己的键（如分布式锁）不出现：

| 命名空间 | 能否改写值 |
| --- | --- |
| `access:` | 否（鉴权相关） |
| `identity:` | 否 |
| `tenancy:` | 否 |
| `openplatform:` | 否 |
| `notification:` | 是 |

不可改写的命名空间仍可删除，删除后缓存按需重建。

缓存管理（Maintenance，平台侧）：

| 操作 | 权限码 | 说明 |
| --- | --- | --- |
| `caches.namespaces` | `maintenance.caches.read` | 已登记的命名空间 |
| `caches.keys` | `maintenance.caches.read` | 按前缀列键，分页续取（`continuation`） |
| `caches.inspect` | `maintenance.caches.read` | 查看值、类型与剩余存活 |
| `caches.update` | `maintenance.caches.update` | 改写值（只对可改写的命名空间） |
| `caches.delete` / `caches.delete-by-prefix` | `maintenance.caches.delete` | 按键或按前缀删除 |

进程内模式下框架的对象缓存与字节存储是分开的两张表，`caches.keys` 两层合起来列（先对象缓存、再字节存储），每个键标出所在的层；Redis 下对象条目序列化进字节存储，只有一层；列键是 SCAN，一段可能一个都没有，`caches.keys` 在操作里接着列到攒满一页（扫空一段提示值加倍、最多 100 次往返），续页标记照旧，同一个键可能出现在两页、同一页内去重。

## 异步与后台工作

6.0 没有自建的「后台任务框架」，异步工作分四类：

| 类型 | 机制 | 例子 |
| --- | --- | --- |
| 定时作业 | 框架 `Scheduling.Database`：`AddJob<T>` 代码登记、`IJobScheduler.ScheduleAsync` 运行时登记 | 日志保留清理、OAuth 清理、分片上传过期，见[定时任务](./scheduling) |
| 托管能力包 | `IHostedFeature` 随应用启停的循环 | Notification 的发件箱发送循环与 Telegram 接收；Auditing 的日志批量写入 |
| 有界队列 | 调用里只入队，后台批量落库 | 七类分表日志（`BasicApp:Auditing:QueueCapacity` / `BatchSize` / `FlushIntervalSeconds`） |
| 提交后推送 | `RealtimeDispatchMiddleware` 暂存，调用成功才发 | `ForceLogout`、`ReceiveNotification`、`TaskProgress` |
| 提交后失效 | `CacheInvalidationMiddleware` 暂存，工作单元结束后删键 | Access 的授权类缓存、会话门禁、套餐白名单 |

### 发件箱

- 宿主装了框架的 `OutboxFeature`：领域事件与业务写入同事务派发（目前各包尚未登记领域事件）。
- 邮件、短信、机器人消息的「待发送记录」是应用表（`SysEmail`、`SysSms`、`SysBotMessage`），本身即可查询、可重发的发件箱：发布通知时经 `Contracts` 的 `IMessageSender` 直接入队，发送循环按 `BasicApp:Notification:DispatchInterval` 轮询，失败按 `RetryDelay` 重试至多 `MaxAttempts` 次，超过 `StuckAfter` 的发送中记录视为卡死重新排队。

### 导出

导出任务（`export-tasks.submit`）在后台以发起人的凭据逐页调用对应的列表操作（权限、租户、数据范围与字段安全照常生效），生成 CSV / XLSX 存成 7 天的临时文件，进度经 `TaskProgress` 推送。见[文件与存储](./file)。

## 下一步

- [定时任务](./scheduling) · [消息通知](./messaging) · [实时推送](./realtime)
