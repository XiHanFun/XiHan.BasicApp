来源：https://basicapp.docs.xihanfun.com/backend/upgrade

# 升级与迁移

BasicApp 已接入 [XiHan.Framework.Upgrade](https://framework.docs.xihanfun.com/guide/upgrade) 的版本状态、迁移台账、多租户分发、租约锁与维护模式扩展，并在 `WebHost/UpdateScripts` 保存前向 SQL 脚本。`XiHan:Upgrade:EnableAutoCheckOnStartup` 为 `true` 时，**应用启动会自动执行待执行的脚本**，失败即中断启动。

## 当前结论

| 能力 | 当前状态 |
| --- | --- |
| Framework Upgrade 模块与引擎 | 已注册 |
| `SysVersion` 版本状态 | 已落库，每个数据库各自维护 |
| `SysMigrationHistory` 台账存储与查询页 | 已接线；每执行一个脚本记一条 |
| 文件系统脚本发现 | 已接入，目录为 `UpdateScripts/<版本>/` |
| 平台库 + 独立租户库分发 | 已实现 |
| 数据库租约锁 | 已实现 |
| 维护模式中间件 | 已实现 |
| 启动时自动执行 SQL | **`EnableAutoCheckOnStartup=true` 时执行**；框架缺省为 `true`，开发配置也写的是 `true` |
| 新建库登记基线 | 本次启动从零建出的平台库、`InitializeDatabase` 新建的独立库，建好即登记为最新脚本版本，不补跑历史脚本 |

::: danger 启动即迁移，失败即中断
启动时有两个执行点，都调用 `IUpgradeEngine.ExecuteAsync()`、都以 `EnableAutoCheckOnStartup` 为开关：

1. **数据库初始化的升级段**：`SaasSchemaUpgrader` 实现框架的 `IDbSchemaUpgrader`，`DbInitializer` 按「建库建表 → 升级脚本 → 播种」三段执行，让存量表的新列在种子读写之前补齐。这一段只在 `EnableDbInitialization` 与 `EnableTableInitialization` 都开启时运行。
2. **应用初始化之后**：`XiHanUpgradeModule.OnPostApplicationInitializationAsync` 先确保版本行存在，再执行一次引擎。数据库初始化关闭时由这里执行；第 1 段已经升级过时这里版本已是最新，直接空转。

两处在引擎返回失败时都抛出「数据库升级失败，已中断启动」，应用不会带着半套表结构对外服务。不希望应用启动时改库，就把 `EnableAutoCheckOnStartup` 设为 `false`，由发布流程先执行脚本再启动应用。
:::

## 代码落点

| 职责 | 位置 / 类型 |
| --- | --- |
| 框架模块 | `XiHanBasicAppCoreModule` 依赖 `XiHanUpgradeModule` |
| 版本与台账存储 | `SaasUpgradeVersionStore` |
| 数据库租约锁 | `SaasUpgradeLockProvider` |
| 多租户列表 | `SaasUpgradeTenantProvider` |
| SQL 执行 | `SaasUpgradeMigrationExecutor` |
| 初始化升级段 | `SaasSchemaUpgrader`（`IDbSchemaUpgrader`：登记新库基线，按开关执行引擎） |
| 维护模式 | `BasicAppUpgradeMaintenanceModeManager` + `MaintenanceModeMiddleware` |
| 脚本目录 | `backend/src/main/XiHan.BasicApp.WebHost/UpdateScripts` |
| 版本页面 | `/setting/version` |
| 升级记录页面 | `/log/migration` |

BasicApp 在 `AddSaasDomainServices()` 中注册四个数据库适配器，使单服务解析优先使用业务实现：版本与台账写入业务库，SQL 由当前租户上下文解析出的 SqlSugar 客户端执行。

## 全新数据库与存量数据库

两条路径职责不同：

| 场景 | 机制 | 负责内容 |
| --- | --- | --- |
| 全新数据库 | SqlSugar CodeFirst + Seeder | 建库、建表、基础数据（演示数据按开关） |
| 存量数据库 | Upgrade + `UpdateScripts` | 增删列、索引变化、数据修复与版本推进 |

`DbInitializer` 对已存在的表不会自动补列。实体结构变更如果只改 C#、不写前向 SQL，存量库会在查询时出现 `column does not exist` 一类错误。

## 脚本约定

目录结构是**一个版本一个子目录**（框架的 `FileSystemUpgradeScriptProvider` 只扫子目录），当前形如：

```text
UpdateScripts/
├── 3.10.0/3.10.0.sql
├── 3.10.1/3.10.1.sql
├── …
├── 5.4.0/
│   ├── 5.4.0-page-codes.sql
│   └── 5.4.0.sql
├── 5.6.0/5.6.0.sql
└── README.md
```

规则：

1. 目录名即版本号，如 `UpdateScripts/5.6.1/5.6.1.sql`。只有高于库中 `DbVersion` 的版本会执行，与程序版本 `backend/props/version.props` 无关。
2. 当前脚本使用 **PostgreSQL 方言**；文档中的 MySQL / MariaDB 支持主要指 ORM 与首次 CodeFirst，不代表现有升级脚本可直接跨库运行。引擎不区分方言，会把脚本原样交给当前库执行：非 PostgreSQL 的存量库开着 `EnableAutoCheckOnStartup` 升级时，`DO $$ … $$` 这类 PostgreSQL 专有语法会执行失败并中断启动，需要自备对应方言的脚本，或关掉自动升级另行处理。
3. PostgreSQL 标识符使用小写且不加引号，匹配 SqlSugar 实际创建的表列名。
4. 尽量用 `IF EXISTS` / `IF NOT EXISTS` 写成可重试脚本。
5. 一个版本内若存在多个脚本，引擎按脚本名排序；版本之间按语义版本升序执行。
6. 先在生产数据副本验证脚本、备份与回滚方案，再发布应用。

示例：

```sql
-- 5.6.1/5.6.1.sql
ALTER TABLE sys_example
    ADD COLUMN IF NOT EXISTS remark varchar(500);

CREATE INDEX IF NOT EXISTS ix_sys_example_tenant_id
    ON sys_example (tenant_id);
```

## 引擎执行流程

启动时的两个执行点调用的是同一台 `UpgradeEngine`，它对每个目标数据库执行：

```text
解析应用版本与脚本
  → 读取/创建 SysVersion
  → 比较 DbVersion 与最新脚本版本
  → 获取数据库租约锁
  → 标记 IsUpgrading
  → 进入维护模式
  → 按版本和脚本名执行未成功记录过的脚本
  → 写 SysMigrationHistory 并推进 DbVersion
  → 更新 AppVersion
  → 退出维护模式并释放锁
```

`HasMigrationHistoryAsync(version, scriptName)` 只跳过已有**成功**记录的脚本；失败记录不会阻止下一次重试。

### 多租户

`EnableMultiTenantIsolation=true` 时，目标顺序是：

1. 平台库；
2. `IsolationMode=Database` 且 `ConfigStatus=Configured` 的租户独立库。

字段隔离租户与平台共库，不重复执行。每个独立库都有自己的 `SysVersion` 与 `SysMigrationHistory`，数据库版本可以独立追踪。

新建的库不补跑历史脚本：本次启动从零建出全部实体表的平台库（`SaasSchemaUpgrader` 按 `DbSchemaUpgradeContext.IsFresh` 判定）与 `InitializeDatabase` 新建的独立库，建好即通过 `IUpgradeEngine.BaselineAsync` 登记为最新脚本版本。独立库里只有租户库实体的表，脚本改平台库表（`[PlatformDataSource]` 实体）的语句要先判表存在，见 `UpdateScripts/README.md`。

### 租约锁

`SaasUpgradeLockProvider` 通过条件更新 `SysVersion.IsUpgrading` 抢占执行权，并用 `UpgradeStartTime + LockExpirySeconds` 回收崩溃节点遗留的锁。它不是 Redis 锁，也不是 PostgreSQL 会话级建议锁。

`PrimaryNodeName` 非空时，只有节点名完全匹配的实例会执行；留空时所有节点均可竞争数据库租约。`NodeName` 留空则由机器名与应用实例 ID 组合生成。

非主节点、以及没抢到租约的节点**不会等待**：引擎直接返回「等待升级 / 锁已被占用」，这不算失败，该节点照常完成启动。多副本同时发布时，要靠发布编排保证升级节点完成之前其它副本不接流量。

### 维护模式

引擎进入维护模式后，本节点的大部分请求返回：

```json
{
  "code": 503,
  "message": "系统正在升级维护，请稍后重试。"
}
```

响应带 `Retry-After: 30`。`/health` 与 `/.well-known/` 被放行，保证编排探针和 OIDC 发现/JWKS 可继续访问。

::: warning 维护状态只在当前进程内生效
`MaintenanceModeState` 是进程内单例。多副本部署时，执行迁移的节点进入维护模式，不会自动让其它节点一起停流。若迁移与旧版本不兼容，应在网关或发布编排层统一摘流，不能只依赖该中间件。
:::

## 执行方式怎么选

当前默认是第一种；要换成别的方式，先关掉 `EnableAutoCheckOnStartup`：

| 方式 | 适用场景 | 注意 |
| --- | --- | --- |
| 启动时自动执行（当前默认，`EnableAutoCheckOnStartup=true`） | 单体或单副本先行的发布 | 失败即中断启动；多副本时非主节点和抢不到租约的节点不等待，需编排层控制放流 |
| 发布流水线独立迁移步骤（`EnableAutoCheckOnStartup=false`） | 多副本生产 | 迁移成功后再放应用流量，边界最清晰；仓库没有现成的独立迁移命令，需要自建 |
| 管理端调用 `IUpgradeCoordinator.StartAsync()` | 人工触发 | BasicApp 没有接这个入口；协调器后台运行且只记日志，不能形成启动失败门禁 |

不要在多个位置同时触发。无论选择哪种方式，都要保证只有一个可审计入口，并测试多节点竞争、脚本失败、进程中断和重复执行。

## 页面与权限

- `/setting/version`：查看 `SysVersion`，只读；写端点已移除。
- `/log/migration`：查看脚本版本、脚本名、成功状态、执行时间、节点与错误信息。
- 两页读取都要求 `saas:version:read`；版本列表导出使用 `saas:version:export`。

页面展示的是当前租户上下文解析到的数据库状态。排查独立租户库时要先确认当前租户，不能只看平台库记录。

## 发布检查清单

1. 抬高应用版本，并新增同版本或更低的必要 SQL 脚本。
2. 确认脚本为 PostgreSQL 小写无引号标识符，且可安全重试。
3. 在生产副本依次验证升级、重复执行和失败恢复。
4. 备份平台库与全部数据库隔离租户库。
5. 确认生产环境的 `EnableAutoCheckOnStartup`：缺省为 `true`，新版本启动即执行脚本；关掉时要在启动前由发布流程完成迁移。
6. 多节点发布时确认 `NodeName` / `PrimaryNodeName`、租约时长与统一摘流方案。
7. 升级后检查 `/setting/version`、`/log/migration`、应用日志与关键业务查询。

## 相关页面

- [数据库配置](./database)：CodeFirst、连接与存量结构变更
- [健康检查与可观测性](./health-observability)：维护窗口仍放行的 `/health`
- [配置参考](../configuration#xihan-upgrade)：`XiHan:Upgrade` 字段
- [部署](../deployment)：发布、进程守护与生产配置
- [Framework 升级与迁移](https://framework.docs.xihanfun.com/guide/upgrade)：引擎抽象与通用流程
