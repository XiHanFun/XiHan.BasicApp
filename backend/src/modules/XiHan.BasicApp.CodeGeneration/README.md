# XiHan.BasicApp.CodeGeneration

## 概述
XiHan.BasicApp.CodeGeneration 提供代码生成领域能力，包含模板、数据源、表结构与生成历史等领域模型，并提供必要的数据种子支持。

## 核心能力
- 代码生成相关领域模型（模板、数据源、表结构、历史）
- 代码生成基础枚举与领域约定
- 代码生成菜单、权限等基础数据种子

## 架构与职责
- Domain：代码生成领域实体与枚举
- Seeders：代码生成相关菜单与权限数据
- Extensions：模块内服务注册与装配

## 依赖关系
- `XiHanBasicAppRbacModule`

## 配置与约定
- 代码生成相关资源依赖 RBAC 权限体系
- 种子数据由模块启动时统一注册

## 使用方式
```csharp
[DependsOn(typeof(XiHanBasicAppCodeGenerationModule))]
public class MyModule : XiHanModule
{
}
```

## 目录结构
```text
XiHan.BasicApp.CodeGeneration/
  README.md
  XiHanBasicAppCodeGenerationModule.cs
  Domain/
    Entities/
    Enums/
  Seeders/
  Extensions/
    ServiceCollectionExtensions.cs
```
