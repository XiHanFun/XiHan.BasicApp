来源：https://basicapp.docs.xihanfun.com/backend/upgrade

# 表结构升级

本页说明正式库的表结构如何建立与升级、升级脚本怎么写，以及版本与升级记录的查看方式，适用于部署人员与修改实体的后端开发者。

开发库与正式库建表的方式不同：

| 库 | 建表方式 |
| --- | --- |
| 开发库 | Development 打开 `XiHan:Persistence:SqlSugar:InitTables`，启动时按实体建缺表、加缺列（另补软删除实体的部分唯一索引） |
| 正式库 | 只认 `Schema/<版本>/*.sql` 与版本台账，由 `SchemaUpgrader` 执行；`InitTables` 保持关闭 |

## 机制

| 项 | 说明 |
| --- | --- |
| 脚本 | `backend/src/XiHan.BasicApp.Host/Schema/<版本>/*.sql`：每个子目录是一个版本（`主.次.修订`，目录名不是版本号即报错），PostgreSQL 方言、`CREATE TABLE IF NOT EXISTS` 风格、可重复执行；目录里的 `.sql` 按文件名顺序执行。目录可用 `BasicApp:Schema:Directory` 改（相对路径按程序目录解析） |
| 基线 | 最低的版本目录：`Schema/6.0.0/0001-baseline.sql`，由 `SchemaBaselineTests` 按 InitTables 建出的结构生成，包含应用实体与框架 Provider 的表（`tenant_record`、`permission_grant_record`、`audit_record`、`message_translation`、`outbox_message`、调度五张表、工作流九张表） |
| 台账 | 每个库自己的 `sys_version`（结构版本、应用版本、是否升级中、升级节点与开始时间）与 `sys_migration_history`（每个脚本的执行记录：版本、脚本、节点、成败、耗时、错误） |
| 执行器 | Runtime 的 `SchemaUpgrader`，租户独立库由 `TenantSchemas` 升级 |

### 执行顺序

1. 打开 `BasicApp:Schema:CreateDatabase`（开发环境打开，缺省关）时，平台库不存在即先建库；台账两张表再按实体建好；
2. 经台账行的条件更新抢升级锁（时间按库的 `now()`，各节点时钟不必对齐）；锁过了 `BasicApp:Schema:LockExpiryMinutes`（缺省 30 分钟）即视为持锁节点已退出，别的节点可以接手；等别的节点升级最多 `LockWaitMinutes`（缺省 10 分钟），等不到即升级失败；
3. 新库从基线建起，之后每个版本一个目录，只执行比台账新的；每个脚本一个事务，成功与失败都记进升级记录，失败即停；库的结构版本比应用带的脚本还新即 `RUNTIME_VALIDATION_004`（不能用旧版本的应用连新库）；
4. 平台库升完后，在同一把锁里逐个升级已就绪的租户独立库：只跑 `Schema/<版本>/tenant/*.sql`，台账在各自的库里，停用的租户也升；没有租户脚本的版本只推进台账。

升级期间，已在运行的其他节点每 10 秒读一次台账，看到有节点持着没过期的锁即进入维护模式：除 `/health` 与 `/.well-known/` 外一律 503（`RUNTIME_UNAVAILABLE_003`，带 `Retry-After`）。

只支持 PostgreSQL：别的库（测试用的 SQLite）跳过脚本，由 InitTables 建表。

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

`tenants.initialize-database` 在租户的分片上建库、只建租户库实体的表（按当前实体建即是最新结构），并在该库的台账登记当前结构版本；之后随平台的升级只跑 `tenant/` 下的脚本。见[多租户 SaaS](./multi-tenancy#开通三步)。

## 写升级脚本

改了实体（加表、加列、改长度）时：

1. 首个正式版发布之前，直接重生成基线：设 `XIHAN_TEST_POSTGRES`（服务器连接串，不带库名）与 `XIHAN_UPDATE_SCHEMA_BASELINE=1` 跑 `SchemaBaselineTests.Baseline_BuildsTheSameTablesAsInitTables_AndRunsOnce`，测试自建临时库、按 InitTables 写回 `0001-baseline.sql`，再校验基线建出的库与 InitTables 一致；只提交基线里属于本次改动的变化。
2. 正式版发布之后，每个版本一个新目录（`Schema/<新版本>/`），新增脚本、文件名带序号：
   - 写成可重复执行（`CREATE TABLE IF NOT EXISTS`、`ADD COLUMN IF NOT EXISTS` …），只用 PostgreSQL 方言；
   - 影响租户库实体的，另在 `Schema/<新版本>/tenant/` 写一份。
3. 开发库由 InitTables 自动对齐，但正式环境只执行脚本，两者必须一致，`SchemaBaselineTests` 守着基线与实体的一致性。

::: tip 给已有行的表加列
开发库的 InitTables 只能给已有行的表加可空列，或数值 / 枚举 / 布尔 / `Guid` 这类能按 CLR 缺省值回填的非可空列；非可空的字符串与日期时间列在实体里声明成可空，或者重建开发库。
:::

::: tip 新的唯一索引建不出来
实体或框架的表多了唯一索引时，开发库里已有重复数据会让 InitTables 建不出索引，宿主启动即失败。开发库重建后重新 `seed`，或按报错的表去掉重复行再启动。
:::

## 版本与升级记录

Maintenance 包提供只读的管理面（平台侧，页面在「系统管理 › 系统服务」下）：

| 操作 | 权限码 | 说明 |
| --- | --- | --- |
| `versions.list` / `get` | `maintenance.versions.read` | 每个库一行（平台库为 `platform`，另加各独立库租户，按库名取）：应用版本、结构版本、是否升级中、升级节点；读不到的库带错误说明 |
| `migrations.list` / `get` | `maintenance.migrations.read` | 脚本执行台账：版本、脚本、节点、成败、耗时 |

## 下一步

- [数据库配置](./database) · [部署](../deployment) · [健康与可观测性](./health-observability)
