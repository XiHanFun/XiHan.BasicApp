# XiHan.BasicApp.WebHost

## 概述
XiHan.BasicApp.WebHost 是基础应用的 Web 主机项目，负责应用入口、模块组合与运行时启动配置。

## 核心能力
- 应用入口与启动流程封装
- JWT 认证与授权配置
- CORS 跨域策略配置
- 组合业务模块（RBAC、升级、代码生成等）

## 架构与职责
- 入口层：负责 Web 应用启动与生命周期管理
- 配置层：统一加载并应用运行时配置
- 模块组合：聚合业务模块与 Web 基础模块

## 依赖关系
- `XiHanBasicAppWebCoreModule`
- `XiHanBasicAppRbacModule`
- `XiHanBasicAppUpgradeModule`
- `XiHanBasicAppCodeGenerationModule`

## 配置与约定
- JWT 配置节：`XiHan:Authentication:Jwt`
- CORS 白名单在模块内配置
- 运行环境配置文件：`appsettings.{Environment}.json`

## 使用方式
```csharp
var builder = WebApplication.CreateBuilder(args);
await builder.AddApplicationAsync<XiHanBasicAppWebHostModule>();

var app = builder.Build();
await app.InitializeApplicationAsync();
await app.RunAsync();
```

## 目录结构
```text
XiHan.BasicApp.WebHost/
  README.md
  Program.cs
  XiHanBasicAppWebHostModule.cs
  appsettings.json
  appsettings.Development.json
  appsettings.Staging.json
  appsettings.Production.json
  Properties/
    launchSettings.json
```
