# 升级与迁移

BasicApp 已接入 [XiHan.Framework.Upgrade](https://framework.docs.xihanfun.com/guide/upgrade) 的版本状态、迁移台账、多租户分发、租约锁与维护模式扩展，并在 `WebHost/UpdateScripts` 保存前向 SQL 脚本。本页区分**已经接线的基础能力**与**当前尚未接线的执行入口**，避免把“引擎可做什么”误写成“应用启动时已经做了什么”。

## 当前结论

| 能力 | 当前状态 |
| --- | --- |
| Framework Upgrade 模块与引擎 | 已注册 |
| `SysVersion` 版本状态 | 已落库，每个数据库各自维护 |
| `SysMigrationHistory` 台账存储与查询页 | 已接线；只有引擎实际执行后才会产生记录 |
| 文件系统脚本发现 | 已接入，目录为 `UpdateScripts` |
| 平台库 + 独立租户库分发 | 已实现 |
| 数据库租约锁 | 已实现 |
| 维护模式中间件 | 已实现 |
| BasicApp 调用 `IUpgradeCoordinator.StartAsync()` / `IUpgradeEngine.ExecuteAsync()` | **当前没有调用入口** |
| 启动时自动执行 SQL | **当前不会发生** |

::: danger `EnableAutoCheckOnStartup` 目前不等于“自动执行迁移”
`XiHanUpgradeModule.OnPostApplicationInitializationAsync` 当前只调用 `IUpgradeStatusService.EnsureInitializedAsync()`，作用是确保版本行存在；它不会调用升级协调器或引擎。BasicApp 源码中也没有其它 `IUpgradeCoordinator` / `IUpgradeEngine` 调用方。

因此当前版本即使 `EnableAutoCheckOnStartup=true`，也只会初始化状态，不会执行 `UpdateScripts`、不会进入维护模式，也不会因 SQL 失败阻止启动。需要上线自动迁移前，必须先补一个明确的执行入口并定义失败策略。
:::

## 代码落点

| 职责 | 位置 / 类型 |
| --- | --- |
| 框架模块 | `XiHanBasicAppCoreModule` 依赖 `XiHanUpgradeModule` |
| 版本与台账存储 | `SaasUpgradeVersionStore` |
| 数据库租约锁 | `SaasUpgradeLockProvider` |
| 多租户列表 | `SaasUpgradeTenantProvider` |
| SQL 执行 | `SaasUpgradeMigrationExecutor` |
| 维护模式 | `BasicAppUpgradeMaintenanceModeManager` + `MaintenanceModeMiddleware` |
| 脚本目录 | `backend/src/main/XiHan.BasicApp.WebHost/UpdateScripts` |
| 版本页面 | `/setting/version` |
| 升级记录页面 | `/log/migration` |

BasicApp 在 `AddSaasDomainServices()` 中注册四个数据库适配器，使单服务解析优先使用业务实现：版本与台账写入业务库，SQL 由当前租户上下文解析出的 SqlSugar 客户端执行。

## 全新数据库与存量数据库

两条路径职责不同：

| 场景 | 机制 | 负责内容 |
| --- | --- | --- |
| 全新数据库 | SqlSugar CodeFirst + Seeder | 建库、建表、系统基线与演示数据 |
| 存量数据库 | Upgrade + `UpdateScripts` | 增删列、索引变化、数据修复与版本推进 |

`DbInitializer` 对已存在的表不会自动补列。实体结构变更如果只改 C#、不写前向 SQL，存量库会在查询时出现 `column does not exist` 一类错误。

## 脚本约定

当前目录内已有：

```text
UpdateScripts/
├── 3.10.0.sql
├── 3.10.1.sql
└── README.md
```

规则：

1. 文件名使用语义版本号，如 `3.10.2.sql`，并与 `backend/props/version.props` 一起升级。
2. 当前脚本使用 **PostgreSQL 方言**；文档中的 MySQL / MariaDB 支持主要指 ORM 与首次 CodeFirst，不代表现有升级脚本可直接跨库运行。
3. PostgreSQL 标识符使用小写且不加引号，匹配 SqlSugar 实际创建的表列名。
4. 尽量用 `IF EXISTS` / `IF NOT EXISTS` 写成可重试脚本。
5. 一个版本内若存在多个脚本，引擎按脚本名排序；版本之间按语义版本升序执行。
6. 先在生产数据副本验证脚本、备份与回滚方案，再发布应用。

示例：

```sql
-- 3.10.2.sql
ALTER TABLE sys_example
    ADD COLUMN IF NOT EXISTS remark varchar(500);

CREATE INDEX IF NOT EXISTS ix_sys_example_tenant_id
    ON sys_example (tenant_id);
```

## 引擎被调用后的流程

执行入口接通后，`UpgradeEngine` 对每个目标数据库执行：

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

### 租约锁

`SaasUpgradeLockProvider` 通过条件更新 `SysVersion.IsUpgrading` 抢占执行权，并用 `UpgradeStartTime + LockExpirySeconds` 回收崩溃节点遗留的锁。它不是 Redis 锁，也不是 PostgreSQL 会话级建议锁。

`PrimaryNodeName` 非空时，只有节点名完全匹配的实例会执行；留空时所有节点均可竞争数据库租约。`NodeName` 留空则由机器名与应用实例 ID 组合生成。

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

## 执行入口怎么选

当前应用没有执行入口。补接线时应先明确策略：

| 策略 | 适用场景 | 注意 |
| --- | --- | --- |
| 发布流水线独立迁移步骤 | 生产推荐 | 迁移成功后再放应用流量，边界最清晰 |
| 宿主启动时同步调用 `IUpgradeEngine.ExecuteAsync()` | 单体或严格启动门禁 | 必须检查 `UpgradeStartResult.Status`，失败时显式终止启动 |
| 管理端调用 `IUpgradeCoordinator.StartAsync()` | 人工触发 | 协调器后台运行且只记日志，不能天然形成启动失败门禁 |

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
5. 确认生产环境实际使用的升级触发入口；当前源码默认没有入口。
6. 多节点发布时确认 `NodeName` / `PrimaryNodeName`、租约时长与统一摘流方案。
7. 升级后检查 `/setting/version`、`/log/migration`、应用日志与关键业务查询。

## 相关页面

- [数据库配置](./database)：CodeFirst、连接与存量结构变更
- [健康检查与可观测性](./health-observability)：维护窗口仍放行的 `/health`
- [配置参考](../configuration#xihan-upgrade)：`XiHan:Upgrade` 字段
- [部署](../deployment)：发布、进程守护与生产配置
- [Framework 升级与迁移](https://framework.docs.xihanfun.com/guide/upgrade)：引擎抽象与通用流程
