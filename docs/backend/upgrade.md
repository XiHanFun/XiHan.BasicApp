# 表结构升级

本页说明库的表结构如何建立与升级、什么时候要写升级脚本、脚本怎么写，以及版本与升级记录的查看方式，适用于部署人员与修改实体的后端开发者。

表结构由 ORM 按实体建出与补齐，各种库走同一条路径；升级脚本只做 ORM 做不了的事。

| 库 | 建表方式 |
| --- | --- |
| 开发库 | Development 打开 `XiHan:Persistence:SqlSugar:InitTables`：每次启动按实体建缺表、加缺列（另补软删除实体的部分唯一索引），改了实体重启即生效 |
| 正式库 | 表结构升级器（`SchemaUpgrader`）在启动时执行：新库按实体建全部表；已有库先执行比台账新的升级脚本，再按实体补缺的表与列；`InitTables` 保持关闭 |

## 机制

| 项 | 说明 |
| --- | --- |
| 按实体建表 | 新库建出全部已登记实体的表（含框架 Provider 的 `tenant_record`、`permission_grant_record`、`audit_record`、`message_translation`、`outbox_message`、调度与工作流的表），再补软删除实体的部分唯一索引；已有库只建缺的表、加缺的列，不改、不删已有的列。只在新库、执行了脚本或应用版本变了时做，同一版本的节点重启不再逐表核对 |
| 升级脚本 | `backend/src/XiHan.BasicApp.Host/Schema/<版本>/<库类型>/*.sql`：每个子目录是一个版本（`主.次.修订`），其下按库类型分目录（`postgresql`、`mysql`、`sqlserver`、`oracle`，测试用 `sqlite`），租户独立库的脚本在库类型目录下的 `tenant/` 里；按文件名顺序执行。目录名不是版本号、库类型写错、脚本没放进库类型目录都报错。目录可用 `BasicApp:Schema:Directory` 改（相对路径按程序目录解析），没有脚本时目录可以不存在 |
| 结构版本 | 新库登记为当前结构版本：脚本目录里最高的版本与 `6.0.0` 取大者；执行完某个版本的脚本即推进到那个版本 |
| 台账 | 每个库自己的 `sys_version`（结构版本、应用版本、是否升级中、升级节点与开始时间）与 `sys_migration_history`（每一步的执行记录：版本、脚本或 `orm-sync`、节点、成败、耗时、错误） |
| 执行器 | Runtime 的 `SchemaUpgrader`，租户独立库由 `TenantSchemas` 升级 |

### 执行顺序

1. 打开 `BasicApp:Schema:CreateDatabase`（开发环境打开，缺省关）时，平台库不存在即先建库（Oracle 的库是 DBA 事先建好的用户，不建）；台账两张表再按实体建好；
2. 经台账行的条件更新抢升级锁；锁过了 `BasicApp:Schema:LockExpiryMinutes`（缺省 30 分钟）即视为持锁节点已退出，别的节点可以接手；等别的节点升级最多 `LockWaitMinutes`（缺省 10 分钟），等不到即升级失败。锁的时间取节点时钟，各节点的偏差远小于失效时间即可；
3. 新库按实体建全部表，登记当前结构版本；已有库先执行比台账新的、本库类型的脚本（每个脚本一个事务，成功与失败都记进升级记录，失败即停），再按实体补缺的表与列（记一步 `orm-sync`）。库的结构版本比应用还新即 `RUNTIME_VALIDATION_004`（不能用旧版本的应用连新库）；
4. 平台库升完后，在同一把锁里逐个升级已就绪的租户独立库：执行本库类型目录下 `tenant/` 里的脚本，再按租户库实体补表补列；台账在各自的库里，停用的租户也升。

升级期间，已在运行的其他节点每 10 秒读一次台账，看到有节点持着没过期的锁即进入维护模式：除 `/health` 与 `/.well-known/` 外一律 503（`RUNTIME_UNAVAILABLE_003`，带 `Retry-After`）。

### 执行时机

| 方式 | 说明 |
| --- | --- |
| 启动时 | `BasicApp:Schema:AutoUpgrade`（缺省 `true`）：开始监听之前执行，升完前这个节点不接请求；失败即以日志 `Startup aborted: schema upgrade failed` 中断启动 |
| 命令 | `XiHan.BasicApp.Host schema upgrade`：不启动应用（Feature 一启动就要读写自己的表，新库上升级要排在它们之前），执行后以 JSON 输出报告（`from`、`to`、`applied`），失败即非零退出；关掉 `AutoUpgrade` 时由运维先跑它再启动 |

```bash
dotnet run --project backend/src/XiHan.BasicApp.Host --launch-profile Development -- schema upgrade
```

新库按「`schema upgrade`（或带 `AutoUpgrade` 启动一次）→ `seed [--only <名,...>] [--list]`」初始化，见[部署](../deployment#首次初始化)。脚本由执行器按台账执行，不要手工对库跑。

### 租户独立库

`tenants.initialize-database` 在租户的分片上建库、按实体只建租户库实体的表，并在该库的台账登记当前结构版本；之后随平台一起升级。独立库的库类型可以与平台库不同，脚本按它自己的类型取。见[多租户 SaaS](./multi-tenancy#开通三步)。

## 写升级脚本

加表、加列、加索引都不用写脚本，ORM 会补。要写脚本的只有 ORM 做不了的：

- 改列名、改表名（ORM 会把新名字当成新列或新表）；
- 改列的类型或长度，或把可空改成非空（ORM 不改已有的列）；
- 删列、删表、删索引；
- 搬数据：新列要从旧列算出来、拆表合表、给已有行回填非空字符串或日期时间列。

写法：

1. 每个版本一个新目录，脚本放进各库类型的目录：`Schema/<新版本>/postgresql/0001-xxx.sql`、`…/mysql/…`、`…/sqlserver/…`、`…/oracle/…`，每种可能有旧库的库类型都要有一份（MySQL、SQL Server、Oracle 从 `6.0.1` 起支持，更早的版本目录只有 PostgreSQL 的）；影响租户库实体的，另在各库类型目录的 `tenant/` 里写一份。
2. 脚本在按实体补表补列之前执行：要往新列搬数据的，在脚本里先把列加上。
3. 写成可重复执行（先判断表、列、索引是否存在）：MySQL 与 Oracle 的 DDL 会隐式提交、回滚不了，失败后修好重跑，已执行的部分要能跳过。
4. Oracle 不能一次执行多条语句：语句之间用只有 `/` 的一行隔开，普通语句末尾不写分号，PL/SQL 块照常写。SQL Server 的脚本不要写 `GO`。

::: tip 给已有行的表加列
InitTables 与升级器补列时，只能给已有行的表加可空列，或数值 / 枚举 / 布尔 / `Guid` 这类能按 CLR 缺省值回填的非可空列；非可空的字符串与日期时间列在实体里声明成可空，或者写脚本先加列并回填。
:::

::: tip 新的唯一索引建不出来
实体或框架的表多了唯一索引时，库里已有重复数据会让建索引失败，升级即中断。按报错的表去掉重复行再启动；开发库也可以重建后重新 `seed`。
:::

## 版本与升级记录

Maintenance 包提供只读的管理面（平台侧，页面在「系统管理 › 系统服务」下）：

| 操作 | 权限码 | 说明 |
| --- | --- | --- |
| `versions.list` / `get` | `maintenance.versions.read` | 每个库一行（平台库为 `platform`，另加各独立库租户，按库名取）：应用版本、结构版本、是否升级中、升级节点；读不到的库带错误说明 |
| `migrations.list` / `get` | `maintenance.migrations.read` | 升级记录：版本、脚本或 `orm-sync`、节点、成败、耗时 |

## 下一步

- [数据库配置](./database) · [部署](../deployment) · [健康与可观测性](./health-observability)
