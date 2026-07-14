# XiHan.BasicApp.Web.Core

## 概述
XiHan.BasicApp.Web.Core 提供基础应用的 Web 基础设施能力，整合 Web Core、Web API、文档、网关与实时通信模块，并提供自动版本脚本更新扩展。

## 核心能力
- Web 基础模块集成与应用初始化入口
- 自动版本脚本更新扩展（`UseAutoVersionUpdate`）

## 架构与职责
- Web 模块聚合：整合基础 Web 能力
- Web 扩展：提供统一的应用构建扩展方法

> 数据库初始化（建表 + 种子）由框架 `XiHanDataModule.OnApplicationInitializationAsync` 负责，不在本模块。

## 依赖关系
- `XiHanBasicAppCoreModule`
- `XiHanWebCoreModule`
- `XiHanWebApiModule`
- `XiHanWebDocsModule`
- `XiHanWebRealTimeModule`
- `XiHanWebGatewayModule`

## 自动版本更新（UseAutoVersionUpdate）
已在 `XiHanBasicAppWebHostModule.OnApplicationInitialization` 中接入。

行为约定：
- **执行时机**：WebHost 模块是依赖图的根，其初始化晚于框架 `XiHanDataModule` 的数据库初始化，故脚本执行时表结构与基线数据均已就绪。
- **节点门控**：仅主节点执行（`XiHan:DistributedIds:SnowflakeId:WorkerId == 1`），多节点部署下不会重复跑脚本。
- **脚本来源**：`AppContext.BaseDirectory/UpdateScripts/*.sql`，文件名即版本号（如 `3.6.0.sql`），按语义化版本升序执行。
  脚本需在 WebHost 的 `UpdateScripts/` 目录下，并由 csproj 的 `CopyToOutputDirectory` 随输出/发布一同拷贝。
- **版本记录**：执行结果写入 `AppContext.BaseDirectory/version.txt`（格式 `版本^时间^是否已执行`）。
- **执行范围**：只执行「高于历史版本」且「不高于当前程序版本」的脚本；每个脚本在单个事务中执行，失败回滚且不记录为已执行。
- **全新部署**：无 `version.txt` 时视为最新版本，只落版本号、不跑任何历史脚本（不会在空库上误执行）。

## 使用方式
```csharp
[DependsOn(typeof(XiHanBasicAppWebCoreModule))]
public class MyWebModule : XiHanModule
{
    public override void OnApplicationInitialization(ApplicationInitializationContext context)
    {
        var app = context.GetApplicationBuilder();
        // 执行本地版本升级脚本
        app.UseAutoVersionUpdate();
    }
}
```

## 目录结构
```text
XiHan.BasicApp.Web.Core/
  README.md
  XiHanBasicAppWebCoreModule.cs
  Extensions/
    AutoVersionUpdate.cs
```
