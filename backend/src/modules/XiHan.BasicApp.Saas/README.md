# XiHan.BasicApp.Saas

## 概述
XiHan.BasicApp.Saas 提供基础应用的权限与角色控制能力，采用 DDD 分层组织，覆盖用户、角色、权限、菜单、租户、配置与字典等核心领域。

## 核心能力
- 用户、角色、权限、菜单、租户等核心领域模型
- 应用服务层的权限管理与配置管理能力
- 领域服务与仓储层的规范化实现
- 权限与资源数据的初始化种子

## 架构与职责
- Application：应用服务与 DTO，面向动态 API 暴露
- Domain：领域实体、领域服务与仓储契约
- Infrastructure：仓储实现与数据访问适配
- Seeders：权限与资源初始化
- Extensions：模块内服务注册与装配

## 依赖关系
- `XiHanBasicAppCoreModule`
- `XiHanAuthenticationModule`
- `XiHanAuthorizationModule`

## 配置与约定
- 数据库配置：`XiHan:Data:SqlSugarCore`
- 应用服务实现 `IApplicationService` 以暴露为动态 API
- 领域模型遵循聚合根与边界上下文约定

## 使用方式
```csharp
[DependsOn(typeof(XiHanBasicAppRbacModule))]
public class MyModule : XiHanModule
{
}
```

## 目录结构
```text
XiHan.BasicApp.Saas/
  README.md
  XiHanBasicAppRbacModule.cs
  Application/
    ApplicationServices/
    Dtos/
  Domain/
    DomainServices/
    Entities/
    Enums/
    Repositories/
  Infrastructure/
    Repositories/
  Seeders/
  Extensions/
    ServiceCollectionExtensions.cs
```
