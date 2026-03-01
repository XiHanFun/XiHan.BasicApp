# XiHan.BasicApp.Upgrade

## 概述
XiHan.BasicApp.Upgrade 提供基础应用的升级模块实现，采用 DDD 分层结构，负责升级状态存储、分布式锁与迁移执行的具体实现，并以应用服务形式暴露升级能力。

## 核心能力
- 升级版本与迁移历史的持久化
- 升级分布式锁的 SqlSugar 实现
- 迁移脚本执行器与多租户分发支持
- 升级应用服务与动态 API 暴露

## 架构与职责
- Application：升级应用服务与 DTO，面向动态 API
- Domain：升级相关领域实体与约束
- Infrastructure：SqlSugar 适配器与实现
- Extensions：模块内依赖注册

## 依赖关系
- `XiHanUpgradeModule`（框架升级引擎）

## 配置与约定
- 配置节：`XiHan:Upgrade`
- 连接配置：`XiHan:Data:SqlSugarCore`
- 动态 API 由应用服务自动暴露，路由遵循框架约定

## 使用方式
```csharp
[DependsOn(typeof(XiHanBasicAppUpgradeModule))]
public class MyModule : XiHanModule
{
}
```

## 目录结构
```text
XiHan.BasicApp.Upgrade/
  README.md
  XiHanBasicAppUpgradeModule.cs
  Application/
    ApplicationServices/
      IUpgradeAppService.cs
      Implementations/
        UpgradeAppService.cs
    Dtos/
      SystemVersionDto.cs
      UpgradeStatusDto.cs
      UpgradeStartResultDto.cs
  Domain/
    Entities/
      SysVersionEntity.cs
      SysMigrationHistory.cs
      SysUpgradeLockEntity.cs
  Infrastructure/
    Adapters/
      SqlSugarUpgradeVersionStore.cs
      SqlSugarUpgradeLockProvider.cs
      SqlSugarUpgradeLockToken.cs
      SqlSugarUpgradeMigrationExecutor.cs
      SqlSugarUpgradeTenantProvider.cs
  Extensions/
    ServiceCollectionExtensions.cs
```
