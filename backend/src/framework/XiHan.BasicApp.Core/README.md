# XiHan.BasicApp.Core

## 概述
XiHan.BasicApp.Core 是基础应用的核心模块，提供统一的领域与应用基础类型，并聚合框架通用模块依赖，作为业务模块的基座。

## 核心能力
- 基础实体与审计实体基类（多租户支持）
- 应用层 DTO 基类与分页请求模型
- 统一聚合框架模块依赖，形成基础运行环境

## 架构与职责
- 领域基础：提供通用实体与聚合根基类
- 应用基础：提供 DTO 基类与分页请求基类
- 模块聚合：统一依赖框架核心模块与跨领域能力

## 依赖关系
- 通过 `XiHanBasicAppCoreModule` 聚合框架能力
- 依赖关系以模块类 `DependsOn` 声明为准

## 配置与约定
- 实体基类统一继承多租户审计实体
- DTO 基类统一继承框架 DTO 基类

## 使用方式
```csharp
[DependsOn(typeof(XiHanBasicAppCoreModule))]
public class MyModule : XiHanModule
{
}
```

## 目录结构
```text
XiHan.BasicApp.Core/
  README.md
  XiHanBasicAppCoreModule.cs
  Dtos/
    BasicAppDto.cs
  Entities/
    BasicAppEntity.cs
```
