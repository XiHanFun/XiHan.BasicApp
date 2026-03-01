# XiHan.BasicApp.Web.Core

## 概述
XiHan.BasicApp.Web.Core 提供基础应用的 Web 基础设施能力，整合 Web Core、Web API、文档、网关与实时通信模块，并在启动阶段执行数据库初始化。

## 核心能力
- Web 基础模块集成与应用初始化入口
- 数据库初始化扩展（`UseDbInitializerAsync`）
- 自动版本脚本更新扩展（`UseAutoVersionUpdate`，可选）

## 架构与职责
- Web 模块聚合：整合基础 Web 能力
- 初始化流程：在应用启动时触发数据库初始化
- Web 扩展：提供统一的应用构建扩展方法

## 依赖关系
- `XiHanBasicAppCoreModule`
- `XiHanWebCoreModule`
- `XiHanWebApiModule`
- `XiHanWebDocsModule`
- `XiHanWebRealTimeModule`
- `XiHanWebGatewayModule`

## 配置与约定
- 数据库初始化行为受 `XiHan:Data:SqlSugarCore` 配置影响
- 自动版本更新基于应用目录脚本文件执行

## 使用方式
```csharp
[DependsOn(typeof(XiHanBasicAppWebCoreModule))]
public class MyWebModule : XiHanModule
{
    public override void OnApplicationInitialization(ApplicationInitializationContext context)
    {
        var app = context.GetApplicationBuilder();
        // 数据库初始化
        AsyncHelper.RunSync(async () => await app.UseDbInitializerAsync(initialize: true));
        // 可选：执行本地版本脚本
        // app.UseAutoVersionUpdate();
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
    DbInitializerExtensions.cs
```
