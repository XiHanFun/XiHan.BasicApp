# 系统设置

「系统设置」这一组页面管的是**运行期可调、不需要改代码重新部署的东西**：菜单、字典、参数、业务编号、缓存、服务监控、版本。本页按页面逐项说明数据模型与使用要点。

对应菜单目录 `/setting`（部分页面在 `/file` 目录下）。

## 菜单管理（`/setting/menu`）

页面能看到并微调 `SysMenu` 表，但**它不是事实源**。

::: danger 菜单的事实源在后端代码里
菜单、路由、组件路径、权限码、国际化键统一由 `Application/Pages/PageRegistry.cs` 登记，`SaasMenuSeeder`（`Order=25`）据此播种到 `SysMenu`。

**新增/修改菜单要改 `PageRegistry` 并重新播种**，在页面上手改会在下次播种时被覆盖或产生漂移。菜单管理页的定位是查看结构、调整排序与显隐，不是维护入口。
:::

关键机制见 [后端架构 · 菜单](./introduction#菜单-后端单一事实源)。

## 数据字典（`/setting/dict`）

两级结构：

| 实体 | 关键字段 |
| --- | --- |
| `SysDict` | `DictCode`（编码）、`DictName`、`DictType`、`IsBuiltIn`（内置不可删）、`Status`、`Sort` |
| `SysDictItem` | `DictId`、`ParentId`（**支持树形字典项**）、`ItemCode`、`ItemName`、`ItemValue`、`Metadata`、`IsDefault`、`Status`、`Sort` |

用途是给表单、搜索下拉提供**可运营维护的选项集**（如客户等级、合同类型）。

::: tip 字典 vs 枚举：怎么选
- **枚举**（C# `enum`）：值集由**代码**决定，改动要发版。标签的事实源是后端枚举元数据（`Enums.{culture}.json`），天然多语言。适合状态机、类型判别这类参与业务逻辑的值。
- **字典**（`SysDict`）：值集由**运营**维护，随时增删。适合业务分类、标签这类纯数据的值。

前端 Schema 字段用 `dictionaryCode` 声明，两者的取值路径是统一的——但**别把参与 `switch` 判断的值放进字典**，运营删一条就会打穿代码分支。
:::

字典项树走 Redis 缓存（`basicapp:saas:configuration:dict-tree`），写侧改完必须调 `InvalidateDictionaryAsync()`。

## 参数配置（`/setting/config`）

`SysConfig` 是**运行期系统参数**，与 `appsettings.json` 是两套东西：

| 字段 | 说明 |
| --- | --- |
| `ConfigKey` / `ConfigValue` / `DefaultValue` | 键、当前值、默认值 |
| `ConfigGroup` | 分组，用于页面归类 |
| `ConfigType` | 配置类型（如 `Feature` 功能开关） |
| `DataType` | 值的数据类型（`String` / 数值 / 布尔 等），前端据此渲染控件 |
| `IsBuiltIn` | 内置参数不可删 |
| **`IsEncrypted`** | **加密存储**——密钥类参数落库前加密，读侧不回显明文 |
| `Status` / `Sort` / `Remark` | 常规 |

::: tip 什么该放 appsettings、什么该放 `SysConfig`
- **`appsettings`**：基础设施连接与启动期就要用的东西（数据库连接串、Redis、JWT 密钥、监听端口）。改了要重启。
- **`SysConfig`**：业务开关与运营参数（是否开放注册、单页默认条数、某功能灰度开关）。改了立即生效（走缓存失效），且**可按租户覆盖**。

判断标准：这个值需要在运行期由管理员改吗？需要 → `SysConfig`。
:::

配置值走缓存（`basicapp:saas:config:value`），写侧调 `InvalidateConfigurationAsync(configKey?)`。

## 业务编号（`/setting/numbering`）

给业务单据发号（订单号、合同号、工单号），保证**同一周期内单调递增且不重复**。

`SysNumberingRule` 的构成：

| 字段 | 说明 | 默认 |
| --- | --- | --- |
| `RuleCode` / `RuleName` | 规则编码与名称 | — |
| `Prefix` | 前缀，如 `SO` | — |
| `Separator` | 分隔符 | `-` |
| `DateFormat` | 日期段格式 | `yyyyMMdd` |
| `SerialLength` | 序号位数（左补零） | `4` |
| `ResetCycle` | **重置周期**：按日/月/年/不重置 | `Daily` |
| `TimeZoneId` | **按哪个时区判定周期边界** | `UTC` |
| `AllowTenantUse` | 是否允许租户使用该规则 | `false` |
| `CurrentValue` / `CurrentPeriod` / `CurrentPeriodOrdinal` | 当前值与周期游标（运行时状态） | — |
| `HasAllocated` | 是否已发过号 | — |

生成结果形如 `SO-20260804-0001`。

::: warning 三个容易出事的点
1. **`TimeZoneId` 决定周期边界**。设成 `UTC` 而业务在东八区，那么每天 08:00 才换号段——上线前对齐清楚。
2. **`ResetCycle` 不能往回退**（如从「按日」改成「按年」），会导致已发出的号段与新周期语义冲突，服务层会拒绝。
3. **改规则会影响历史号的可读性**，但不会回溯改已发的号。`SysNumberingAllocation` 记录每次分配，可用于追溯。
:::

发号在事务内以「单调翻转 → 递增 → 读回」完成，不加进程内锁；并发下靠数据库保证。

## 缓存管理（`/setting/cache`）

查看与清理 Redis 缓存。日常用途有两个：

- **排障**：怀疑「改完不生效」时，先看缓存里是不是旧值。
- **应急**：确认是失效逻辑漏了，可以手工清一次先恢复业务，再回头修代码。

::: danger 手工清缓存是应急手段
手工清完问题消失，说明**写侧漏调了失效方法**——这是需要修的 bug，不是一次性故障。正确做法见 [缓存与异步](./caching#加一个新缓存的清单)。
:::

缓存键的命名规则、条目清单与失效器方法同样在那一页。

## 服务监控（`/setting/server`）

`ServerAppService` 提供运行时信息可视化：CPU / 内存 / 磁盘 / GPU / 网络 / 主板 / .NET 运行时。

它读的是**当前进程所在机器**的信息。多实例部署时看到的是「你这次请求恰好打到的那个实例」，不是集群汇总——要做集群视图请接 OpenTelemetry 指标导出。配置与真实健康探测见[健康检查与可观测性](./health-observability)。

## 版本管理（`/setting/version`）

`SysVersion` 记录应用版本与数据库版本，配合框架的升级引擎：

| 字段 | 说明 |
| --- | --- |
| `AppVersion` / `DbVersion` | 应用版本 / 数据库版本 |
| `MinSupportVersion` | 允许升级的最低来源版本 |
| `IsUpgrading` / `UpgradeNode` / `UpgradeStartTime` | 升级中标记、执行节点、开始时间（多节点协调用） |

升级能力由配置节 `XiHan:Upgrade` 控制（分布式锁、主节点、维护模式等），见[升级与迁移](./upgrade)。

::: warning 版本页只读，脚本才是迁移入口
`SysVersion` 由升级引擎独占写入，管理端不提供手工改版本。结构和数据迁移放在 `WebHost/UpdateScripts/{version}.sql`；`DbInitializer` 只负责全新数据库建表，不会给已有表补列。当前 BasicApp 尚未调用升级执行入口，脚本不会仅因 `EnableAutoCheckOnStartup=true` 自动运行，详见[当前结论](./upgrade#当前结论)。
:::

## 通道配置（邮件 / 短信 / 机器人）

这几个页面在同一菜单目录下，配置**落库而非写 appsettings**，因此可以按租户隔离、运行期热切换：

| 页面 | 实体 | 说明 |
| --- | --- | --- |
| 邮件配置 `/setting/email-config` | `SysEmailConfig` | SMTP 网关 |
| 短信配置 `/setting/sms-config` | `SysSmsConfig` | 短信网关 |
| 机器人配置 `/setting/bot-config` | `SysBotConfig` | 钉钉 / 飞书 / 企业微信 Webhook 机器人 |
| Telegram 机器人 `/setting/telegram-bot` | `SysTelegramBot` | 多实例 Telegram Bot |

它们通过 `services.Replace(...)` 覆盖框架各 Bot 包的默认 `*ConfigStore`——框架侧读的是接口，实现被换成了「从数据库读」。密钥类字段加密存储。

发送链路与模板见 [消息中心](./messaging)。

## 相关页面

- [缓存与异步](./caching)：缓存条目与失效器
- [健康检查与可观测性](./health-observability)：服务监控、探针与 OpenTelemetry
- [升级与迁移](./upgrade)：版本状态、脚本与当前执行边界
- [配置参考](../configuration)：`appsettings` 全量配置节
- [消息中心](./messaging)：模板与四通道发送
- [数据模型](./data-model#系统设置)：相关表结构
