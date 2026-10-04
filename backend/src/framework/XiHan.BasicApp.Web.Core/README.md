# XiHan.BasicApp.Web.Core

## 概述
XiHan.BasicApp.Web.Core 提供基础应用的 Web 基础设施能力，整合 Web Core、Web API、文档、网关、实时通信与 MCP 模块，并提供数据库升级期间的维护模式。

## 核心能力
- Web 基础模块集成与应用初始化入口
- 升级维护模式（`BasicAppUpgradeMaintenanceModeManager` + `MaintenanceModeMiddleware`）

## 架构与职责
- Web 模块聚合：整合基础 Web 能力
- 维护模式：把框架升级引擎的进入/退出映射为进程内标志位，维护期间拦截业务请求

> 数据库初始化（建表 + 种子）由框架 `XiHanDataModule.OnApplicationInitializationAsync` 负责，升级脚本由 Saas 与框架 Upgrade 模块执行，均不在本模块。

## 依赖关系
- `XiHanBasicAppCoreModule`
- `XiHanWebCoreModule`
- `XiHanWebApiModule`
- `XiHanWebDocsModule`
- `XiHanWebRealTimeModule`
- `XiHanWebGatewayModule`
- `XiHanWebMcpModule`

## 数据库升级与维护模式
早期的 `UseAutoVersionUpdate`（`version.txt` 记版本、按 `SnowflakeId:WorkerId == 1` 判主节点）已废除。现行机制：

- **脚本位置**：WebHost 的 `UpdateScripts/<版本>/<版本>.sql`，一个版本一个目录，随输出/发布一同拷贝。
- **执行开关**：`XiHan:Upgrade:EnableAutoCheckOnStartup`（框架缺省 `true`）；关闭时启动不执行脚本，须先手工升级再启动。
- **执行点**：两处都调用 `IUpgradeEngine.ExecuteAsync()`：
  1. Saas 的 `SaasSchemaUpgrader`（`IDbSchemaUpgrader`）在 `DbInitializer` 建表之后、播种之前执行，让存量表的新列先于种子补齐；
  2. 框架 `XiHanUpgradeModule.OnPostApplicationInitializationAsync` 在应用初始化之后再执行一次，前一处已升级时空转。
- **状态记录**：库版本记在 `SysVersion`，每个脚本的执行结果记在 `SysMigrationHistory`；本次从零建出的库直接登记为最新版本，不补跑历史脚本。
- **多节点**：由 `SysVersion` 上的数据库租约锁协调执行权，与 `WorkerId` 无关。
- **失败处理**：引擎返回失败即抛出，中断启动。
- **维护模式**（本模块）：引擎执行期间置位 `MaintenanceModeState`，本节点除 `/health`、`/.well-known/` 外的请求返回 503 并带 `Retry-After: 30`；该状态只在当前进程内生效，多副本统一摘流需在网关或发布编排层处理。

权威说明见 [升级与迁移](../../../../docs/backend/upgrade.md) 与 [UpdateScripts/README.md](../../main/XiHan.BasicApp.WebHost/UpdateScripts/README.md)。

## 使用方式
```csharp
[DependsOn(typeof(XiHanBasicAppWebCoreModule))]
public class MyWebModule : XiHanModule
{
}
```

依赖本模块即可：维护模式中间件在本模块的 `OnApplicationInitialization` 中注册，无需另行调用。

## 目录结构
```text
XiHan.BasicApp.Web.Core/
  README.md
  XiHanBasicAppWebCoreModule.cs
  Upgrade/
    MaintenanceMode.cs
```
