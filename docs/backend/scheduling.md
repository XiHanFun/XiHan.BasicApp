# 任务调度

页面 `/setting/job`，实体 `SysTask` + `SysTaskLog`（按月分表）。它是框架 [Tasks 调度引擎](https://framework.docs.xihanfun.com/packages/tasks) 的**管理面**：把任务定义落库、在页面上启停与手动触发，执行仍由框架的调度器完成。

## 数据模型

`SysTask` 的字段分四组：

### 标识与执行目标

| 字段 | 说明 |
| --- | --- |
| `TaskCode` / `TaskName` / `TaskDescription` | 编码、名称、描述 |
| `TaskGroup` | 分组，便于按业务域归类 |
| `TaskClass` | **执行类的类型名**——调度器据此解析出 `IJobWorker` |
| `TaskMethod` | 方法名（按需） |
| `TaskParams` | 参数（JSON 字符串），执行时注入上下文 |

### 触发方式

| 字段 | 说明 | 默认 |
| --- | --- | --- |
| `TriggerType` | `Immediate` / Cron / 间隔 等 | `Immediate` |
| `CronExpression` | Cron 表达式（`TriggerType` 为 Cron 时用） | — |
| `IntervalSeconds` | 固定间隔秒数 | — |
| `StartTime` / `EndTime` | 生效窗口 | — |
| `RepeatCount` | 重复次数上限，**`-1` 表示不限** | `-1` |
| `NextRunTime` / `LastRunTime` / `ExecutedCount` | 运行时游标 | — |

前端用 `CronExpression.vue` 组件（输入框 + 可视化弹窗）编辑 Cron，不必手写表达式。

### 执行控制

| 字段 | 说明 | 默认 |
| --- | --- | --- |
| `TimeoutSeconds` | 单次执行超时 | `300` |
| **`AllowConcurrent`** | **是否允许并发重入** | `false` |
| `RetryCount` / `MaxRetryCount` | 已重试 / 最大重试 | `0` / `3` |
| `Priority` | 优先级 | `0` |
| `RunTaskStatus` | 运行状态（`Pending` / `Running` / …） | `Pending` |
| `Status` | 启用状态 | `Enabled` |

## 启动时同步

Saas 模块在 `OnPostApplicationInitialization`（**所有模块都就绪之后**）做两件事：

1. 扫描代码里声明式定义的任务；
2. 把数据库中**活跃的** `SysTask` 同步进调度器。

同时**复位崩溃残留的 `Running` 状态**——进程被杀时正在跑的任务会永远停在 `Running`，既不会被再次触发（`AllowConcurrent=false` 时会被判为「已在运行」而跳过），也不会自己结束。不复位就等于这个任务从此再也不执行了。

::: warning 新加的任务没跑起来
按顺序查：
1. `Status` 是不是 `Enabled`；
2. `TaskClass` 能不能解析到实际类型（改过命名空间/类名？）；
3. Cron 表达式算不算得出下次时间——**算不出时任务会「注册了但永不执行」**，框架会打 Warning 日志，去日志里搜；
4. `EndTime` 是不是已经过了、`RepeatCount` 是不是已经用完；
5. `RunTaskStatus` 是不是卡在 `Running`（崩溃残留没复位）。
:::

## 并发与多实例

`AllowConcurrent = false` 依赖任务锁防止重入。**跨实例防并发需要 Redis**——框架的锁来自 Caching，未接 Redis 时退化为进程内锁，多实例部署会各跑各的。

生产多实例部署时这一点务必确认，否则会出现「日报表发了三份」这类问题。

## 多租户

任务执行时会切换到正确的租户上下文（`ICurrentTenant.Change`），解析优先级是：参数里的 `tenantId` → `SysTask` 的租户归属 → 当前异步上下文租户。宿主级任务的租户为空。

这意味着**同一个任务类可以被不同租户各配一份**，各自跑在自己的数据范围里。

## 执行日志

`SysTaskLog`（`Sys_Task_Log_{yyyyMM}`，按月分表）记录每次执行的开始/结束、耗时、结果与异常。分表数据要走 SqlSugar 的分表 API 按时间范围查，不能当普通表直接查。

## 与其它异步机制的分工

| 需求 | 用什么 |
| --- | --- |
| **周期性/定时触发**（每天凌晨跑报表） | **本页的任务调度 + `SysTask`** |
| 一次性 fire-and-forget，入队即返回 | 框架 `IBackgroundJobManager.EnqueueAsync` |
| 持续拉队列消费、业务表做状态机 | `XiHanBackgroundServiceBase<T>` + `IRedisDelayQueue<T>`（发件箱、导出走这条） |
| 等人审批的长流程 | [工作流](./workflow) |

## 相关页面

- [框架 · Tasks](https://framework.docs.xihanfun.com/packages/tasks)：调度引擎、中间件管道、声明式特性
- [缓存与异步](./caching)：另外两条异步链路
- [数据模型](./data-model#文件与任务)：表结构
