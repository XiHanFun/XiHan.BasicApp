![logo](./assets/logo.png)

[![Ask DeepWiki](https://deepwiki.com/badge.svg)](https://deepwiki.com/XiHanFun/XiHan.BasicApp)

# XiHan.BasicApp

通用、全面的管理系统，基于 XiHan.Framework 和 XiHan.UI 构建。

## 概述
XiHan.BasicApp 是完整的企业级应用解决方案。采用模块化与 DDD 分层结构，提供权限管理、代码生成与升级能力等基础模块。

## 设计目标
- 面向企业级后台管理场景的通用能力沉淀
- 统一模块边界与依赖关系，便于扩展与维护
- 以应用服务与动态 API 统一接口暴露方式
- 兼容多租户与分布式部署需求

## 架构概览
系统整体分为框架层、功能模块层与主应用层：
- 框架层：提供基础应用能力与 Web 管道能力
- 模块层：承载权限、代码生成与升级等通用模块
- 主应用层：组合模块并提供 WebHost 启动入口

## 项目结构
```text
backend/
  src/
    framework/
      XiHan.BasicApp.Core/
      XiHan.BasicApp.Web.Core/
    modules/
      XiHan.BasicApp.CodeGeneration/
      XiHan.BasicApp.Rbac/
      XiHan.BasicApp.Upgrade/
    main/
      XiHan.BasicApp.WebHost/
```

## 模块清单
| 模块/项目 | 说明 |
| --- | --- |
| XiHan.BasicApp.Core | 基础应用层能力与模块化支持 |
| XiHan.BasicApp.Web.Core | Web 核心能力与管道扩展 |
| XiHan.BasicApp.Rbac | 角色、权限与组织管理模块 |
| XiHan.BasicApp.CodeGeneration | 代码生成与模板管理模块 |
| XiHan.BasicApp.Upgrade | 升级引擎的应用侧实现 |
| XiHan.BasicApp.WebHost | 应用启动入口与模块聚合 |

## 启动方式
```csharp
using XiHan.Framework.Core.Extensions.DependencyInjection;
using XiHan.BasicApp.WebHost;

var builder = WebApplication.CreateBuilder(args);
await builder.AddApplicationAsync<XiHanBasicAppWebHostModule>();

var app = builder.Build();
await app.InitializeApplicationAsync();
await app.RunAsync();
```
