# 表结构升级（不做 5.x 迁移）

::: danger 6.0 不提供 5.x → 6.0 数据迁移
这是方案的明确决定（D12，2026-10-27 拍板）：6.0 的主键（雪花 `long` → UUID v7）、租户 ID（`long` → 字符串）、并发戳、权限码文法与表名**全部改变**，不做原地升级，也**不提供迁移工具**。

- 6.0 实例从空库经 `schema upgrade` + `seed` **全新初始化**；
- 现有 5.x 部署留在 `release/v5` / `hotfix/v5` 维护线继续维护；
- 要换到 6.0 的部署，在 6.0 上**重新录入**租户、账号、角色与业务数据（可借助 6.0 的导入能力或自写脚本，但项目不提供、不支持）；
- 不要把 6.0 指向 5.x 的库：两者的表结构不兼容，基线脚本按「表不存在才建」执行，不会改造已有的同名表，结果不可预期。
:::

本页其余内容讲 6.0 **自身**版本之间的表结构管理。

## 机制

| 项 | 说明 |
| --- | --- |
| 脚本 | `backend-next/src/XiHan.BasicApp.Host/Schema/<版本>/*.sql`，PostgreSQL 方言、`CREATE TABLE IF NOT EXISTS` 风格、可重复执行；目录里的 `.sql` 按文件名顺序执行 |
| 基线 | `Schema/6.0.0/0001-baseline.sql`：由 `SchemaBaselineTests` 按 InitTables 建出的结构生成，包含应用实体与框架 Provider 的表（`tenant_record`、`permission_grant_record`、`audit_record`、调度五张表、`outbox_message`） |
| 台账 | `sys_version`（库的结构版本、应用版本、是否升级中）与 `sys_migration_history`（每个脚本的执行记录，成功失败都记） |
| 执行器 | `Runtime` 的 `SchemaUpgrader` |

### 执行顺序

1. 台账两张表先按实体建好；
2. 经台账行的条件更新**抢升级锁**（时间按库的 `now()`，`BasicApp:Schema:LockExpiryMinutes` / `LockWaitMinutes`），别的节点等它升完再看还有没有要执行的；升级期间别的节点进入维护模式（503）；
3. 新库从最低的基线建起，之后每个版本一个目录，只执行比台账新的；**每个脚本一个事务**，失败即停；
4. 平台库升完后，在同一把锁里逐个升级**已就绪的租户独立库**：只跑 `Schema/<版本>/tenant/*.sql`，台账在各自的库里，停用的租户也升；没有租户脚本的版本只推进台账。

只支持 PostgreSQL：别的库（测试用的 SQLite）跳过脚本，由 InitTables 建表。

### 什么时候执行

| 方式 | 说明 |
| --- | --- |
| 启动时 | `BasicApp:Schema:AutoUpgrade`（缺省 `true`）：宿主启动前执行，失败即以日志 `Startup aborted: schema upgrade failed` 中断启动 |
| 命令 | `XiHan.BasicApp.Host schema upgrade`：命令模式执行并以 JSON 输出报告（`from`、`to`、`applied`） |

```bash
dotnet run --project backend-next/src/XiHan.BasicApp.Host --launch-profile Development -- schema upgrade
```

## 写升级脚本

改了实体（加表、加列、改长度）时：

1. 在下一个版本目录（如 `Schema/6.0.1/`）新增脚本，文件名带序号；
2. 写成**可重复执行**（`IF NOT EXISTS`、`ADD COLUMN IF NOT EXISTS` …），只用 PostgreSQL 方言；
3. 影响租户库实体的，另在 `Schema/<版本>/tenant/` 写一份；
4. 开发库由 InitTables 自动对齐，但正式环境只认脚本——两者必须一致，`SchemaBaselineTests` 守着基线与实体的一致性。

::: tip 给已有行的表加列
开发库的 InitTables 只能给已有行的表加可空列，或数值 / 枚举 / 布尔 / `Guid` 这类能按 CLR 缺省值回填的非可空列；非可空的字符串与日期时间列在实体里声明成可空，或者重建开发库。
:::

## 版本与升级记录

Maintenance 包提供只读的管理面（平台侧）：

| 操作 | 权限码 | 说明 |
| --- | --- | --- |
| `versions.list` / `get` | `maintenance.versions.read` | 每个库一行：应用版本、结构版本、是否升级中、升级节点（目前只列平台库，独立库租户的版本不显示，N2 收尾项） |
| `migrations.list` / `get` | `maintenance.migrations.read` | 脚本执行台账：版本、脚本、节点、成败、耗时 |

## 下一步

- [数据库配置](./database) · [部署](../deployment) · [健康与可观测性](./health-observability)
