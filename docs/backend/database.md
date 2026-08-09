# 数据库配置

连接怎么配、支持哪些库、读写分离怎么开、启动时的建库建表播种、分表怎么查，以及**为什么加字段后部署会炸**。

配置节 `XiHan:Data:SqlSugarCore`。

## 支持的数据库

ORM 是 SqlSugar，`DbType` 取它的枚举值：

| 数据库 | `DbType` | 说明 |
| --- | --- | --- |
| **PostgreSQL** | `PostgreSQL` | **默认与推荐** |
| MySQL / MariaDB | `MySql` | MariaDB 协议兼容，同用 `MySql` |
| SQL Server | `SqlServer` | — |
| Oracle | `Oracle` | 支持但不建议 |
| 达梦 / 人大金仓 | `Dm` / `Kdbndp` | 国产库，驱动随 `SqlSugarCore` 一并安装 |

本地用 Docker 起库见 [开发环境](../dev-environment)。

## 连接配置

```json
{
  "XiHan": {
    "Data": {
      "SqlSugarCore": {
        "ConnectionConfigs": [
          {
            "ConfigId": "1",
            "ConnectionString": "Server=127.0.0.1;Database=XiHanBasicApp;Username=postgres;Password=***;TrustServerCertificate=true;",
            "DbType": "PostgreSQL",
            "IsAutoCloseConnection": true,
            "SlaveConnectionConfigs": []
          }
        ]
      }
    }
  }
}
```

| 键 | 说明 |
| --- | --- |
| `ConfigId` | 连接唯一标识（多库 / 多租户路由用），**字符串** |
| `ConnectionString` | 主库连接串 |
| `DbType` | 见上表 |
| `IsAutoCloseConnection` | 自动关闭连接 |
| `SlaveConnectionConfigs[]` | 从库；空数组 = 单库不分离 |

::: warning 生产密码走环境变量
`XiHan__Data__SqlSugarCore__ConnectionConfigs__0__ConnectionString`。生产 `appsettings` 通常被 gitignore，需在服务器单独维护。
:::

## 读写分离

填了从库后 **SELECT 自动走从库、写与事务走主库**，业务代码无感知：

```json
"SlaveConnectionConfigs": [
  { "ConnectionString": "Server=从库1;Database=XiHanBasicApp;Username=postgres;Password=***;" },
  { "ConnectionString": "Server=从库2;Database=XiHanBasicApp;Username=postgres;Password=***;" }
]
```

::: danger `HitRate` 配不上，别写
`HitRate`（读权重）是 SqlSugar 的**字段**、绑不上 `appsettings`——写了也无效、恒为 0。

框架会把权重为 0 的从库归一化为 `DefaultSlaveHitRate`（默认 `10`），所以**不写 `HitRate` 也能等权分担读**。

需要差异化权重、挂 `ConfigureExternalServices` 或自写探活，用代码钩子 `XiHanSqlSugarCoreOptions.ConfigureConnectionConfigs`。
:::

### 从库健康探针

| 键 | 默认 | 说明 |
| --- | --- | --- |
| `EnableSlaveHealthCheck` | `false` | 周期探活，不可用从库自动摘除读权重 |
| `SlaveHealthCheckIntervalSeconds` | `30` | 探测周期 |
| `SlaveFailureCooldownSeconds` | `120` | 故障冷却窗口，恢复后先冷却再回填权重避免抖动 |
| `DefaultSlaveHitRate` | `10` | 权重归一化默认值 |

## 启动初始化

| 键 | 说明 |
| --- | --- |
| `EnableDbInitialization` | 启动时自动建库（库不存在则创建）；同时是整个初始化流程的总开关，关掉则建表与播种一并跳过 |
| `EnableTableInitialization` | 启动时 CodeFirst 建表 |
| `EnableDataSeeding` | 启动时写入种子数据 |

三个开关都开时，**首次启动一条龙**：建库 → 建表 → 播种。这也是「拿到代码配好连接串就能跑」的原因。

### 建表只建不改

::: danger 加了字段部署后必报「列不存在」
`DbInitializer` **表存在就跳过创建**（日志里是「表已存在，跳过创建」），它**从不为已有表补列**。

给既有实体加字段后部署，运行到该表的查询就会报 `42703 column does not exist`（PG）或等价错误。

三条路：
- 正式版本在 `WebHost/UpdateScripts/{version}.sql` 编写前向迁移（推荐）；
- 可丢弃数据的本地环境重建数据库；
- 紧急修复时手工 `ALTER TABLE`，随后仍要补入版本脚本。

CodeFirst 负责首次建表；已有库的结构和数据变化由 Framework Upgrade 引擎执行。
:::

::: tip 升级状态不是本地文件
升级引擎可用 `SysVersion` 和 `SysMigrationHistory` 记录每个数据库的版本与脚本结果，不使用 `version.txt`；独立数据库租户也可逐库升级。但当前 BasicApp 尚未调用执行入口，启动不会自动跑脚本，详见[升级与迁移](./upgrade)。
:::

## 种子数据

分两类：

| 类别 | 开关 | 内容 |
| --- | --- | --- |
| **系统基线** | 始终播种 | 身份、权限、租户版本、配置、字典、菜单、消息模板、OAuth 应用、通知、存储配置、任务——应用可运行的最小骨架 |
| **演示数据** | `Saas:Seed:EnableDemoData` | 示例组织、演示账号、演示业务租户 |

`EnableDemoData` **缺省或非法值都视为启用**，显式 `false` 才整体跳过。

超管初始密码用 `Saas:Seed:SuperAdminPassword`（环境变量 `Saas__Seed__SuperAdminPassword`）覆盖，**生产必改**。

种子的 `Order` 段与执行顺序见 [框架简介](./introduction#种子数据)。

## 分表查询

按月分表的实体（日志类）物理表名形如 `Sys_Access_Log_20260801`。

::: warning 分表不能当普通表查
读写都必须显式挂 SqlSugar 的分表 API，否则表名里的 `{year}{month}{day}` 占位符不会被替换：

- 查询、插入：`.SplitTable()`；
- 条件删除：`.SplitTable(tabs => tabs)`——无参重载只支持按实体集合删除，条件删除会在运行时抛异常。
:::

## SQL 日志与诊断

| 键 | 默认 | 说明 |
| --- | --- | --- |
| `EnableSqlLog` | `false` | 打印所有 SQL。**生产建议关闭**，日志会爆量 |
| `EnableSqlErrorLog` | `true` | 记录 SQL 异常 |
| `EnableSlowSqlLog` | `true` | 记录慢 SQL |
| `SlowSqlThresholdMilliseconds` | `10000` | 慢 SQL 阈值（毫秒），**纯观测用途、不影响语句执行** |
| `CommandTimeoutSeconds` | `300` | ADO 命令超时；0/负值不覆盖；**须明显大于慢 SQL 阈值** |

排查性能问题的顺序：先看慢 SQL 日志找出目标语句 → 临时开 `EnableSqlLog` 看完整 SQL 与参数 → 定位后关掉。

## 数据变更日志

| 键 | 默认 | 说明 |
| --- | --- | --- |
| `EnableDiffLog` | **`false`** | 实体差异日志（`SysDiffLog`）总开关 |

::: danger 数据变更日志页恒空的头号原因
默认 `false`——不开则 Diff AOP 根本不挂载，收集到的差异被直接丢弃。**生产 `appsettings` 常被 gitignore，最容易漏配的就是这一项。**

代价：开启后 update/delete 会先查一次旧值算差异，**每个写操作多一次 SELECT**。

覆盖范围：只覆盖走仓储的写。绕过仓储直接用 `DbClient` 的写（如 `UpdateColumns`）不产生差异日志。
:::

## 一个仓储层的强制约定

::: danger 仓储里禁止显式调用 `.EnableQueryFilter()`
框架默认 `EnableAutoUpdateQueryFilter` / `EnableAutoDeleteQueryFilter` 均为 `true`，SqlSugar 的 `Updateable<T>()` / `Deleteable<T>()` 工厂内部**已经自动挂了一次**租户/软删过滤。

再显式挂一次 → 同一份过滤烘进 WHERE 两遍 → 生成同名参数 `@constant1001` → 一旦叠加 Diff 的 `GetDiffTable` 重查旧值就崩（MySQL 驱动直接抛，PG 容忍重名故不崩，但仍是冗余死条件）。

`.EnableDiffLogEvent()` 保留，它单独用是安全的。
:::

## 排查

| 现象 | 原因 |
| --- | --- |
| 启动连不上库 | 连接串、防火墙、库是否已启动 |
| 部署后报「列不存在」 | 版本升级脚本缺少对应 `ALTER TABLE` 或未成功执行，见上 |
| 数据变更日志空 | `EnableDiffLog` 没开 |
| 写操作报参数重名 | 仓储里显式调了 `.EnableQueryFilter()` |
| 从库没分担读 | 确认 `SlaveConnectionConfigs` 填了；`HitRate` 写了也无效 |
| 日志查不到数据 | 分表查询没挂 `.SplitTable()` |
| 多节点主键冲突 | `WorkerId` 没逐节点改，见 [实体基类](./entity#主键与并发) |

## 相关页面

- [实体基类](./entity)：列约定、软删、分表声明
- [数据模型](./data-model)：全部数据表清单
- [开发环境](../dev-environment)：Docker 起库与连接串对齐
- [配置参考](../configuration#xihan-data-sqlsugarcore)：全量配置项
