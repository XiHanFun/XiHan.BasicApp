# XiHan.BasicApp.CodeGeneration

## 概述
代码生成模块：以"数据库表/列 + 模板"为元数据，生成全栈代码（实体/DTO/仓储/服务/前端页面）。对齐 `XiHan.BasicApp.Saas` 的分层架构（DDD + CQRS + 多租户），并预留由低代码（代码生成器）演进到零代码（运行时元数据驱动）的能力。

> 详细架构、引擎管线、低代码→零代码路线图与分阶段计划见：[Docs/CodeGeneration-Architecture.md](Docs/CodeGeneration-Architecture.md)

## 当前阶段（S0：架构骨架）
- ✅ 领域实体（数据源/表/列/模板/历史）+ 枚举 + 种子数据
- ✅ 引擎抽象（`Domain/Generation`：渲染/扫描/类型映射/打包/编排契约）
- ✅ 引擎实现骨架（`Infrastructure/Generation`：Scriban 渲染接通、DbFirst 扫描、类型映射、Zip 打包、编排管线）
- ✅ 仓储接口 + 实现（×5）
- ✅ 应用层：基类 + Contracts + DTO + 命令/查询服务空壳 + 编排服务（已接引擎）
- 🟡 CRUD 业务规则（领域服务）、内置模板、前端管理页、零代码运行时 —— 见设计文档分阶段计划（S1~S4）

## 架构与职责
- `Domain/Entities`、`Domain/Enums`：代码生成领域模型
- `Domain/Repositories`：仓储接口（继承 Saas `ISaasRepository<T>`）
- `Domain/Generation`：代码生成引擎契约（本模块核心抽象）
- `Domain/Permissions`：权限码常量（对齐种子 `code_gen:*`）
- `Application/*`：Abstractions（DynamicApi 基类）、Contracts、Dtos、Mappers、AppServices（命令）、QueryServices（查询）
- `Infrastructure/Repositories`：仓储实现（`SaasRepository<T>`）
- `Infrastructure/Generation`：引擎实现（Scriban 渲染 / DbFirst 扫描 / 类型映射 / Zip 打包 / 编排）
- `Seeders`、`Extensions`：种子数据与模块装配

## 依赖关系
- `XiHanBasicAppSaasModule`（多租户/仓储基类/权限/枚举等基础）
- 复用框架：`XiHan.Framework.Templating`（Scriban）、`XiHan.Framework.Data`（DbFirst 元数据）、`DynamicApi`、`VirtualFileSystem`

## 配置与约定
- 模板引擎：保留多引擎抽象，当前仅实现 Scriban（Razor/T4 占位，见设计文档决策 D1）
- 权限：依赖 RBAC，权限码 `code_gen:{read,create,update,delete,export,import,execute}`
- 多租户/软删/审计：实体内建；落盘默认禁用（仅预览/Zip）
- DynamicApi 分组：`BasicApp.CodeGen`（与系统 SaaS 服务区隔）

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
  Docs/CodeGeneration-Architecture.md      # 架构设计文档
  Domain/
    Entities/  Enums/  Permissions/  Repositories/  Generation/
  Application/
    Abstractions/  Contracts/  Dtos/  Mappers/  AppServices/  QueryServices/
  Infrastructure/
    Repositories/  Generation/
  Seeders/  Extensions/
  XiHanBasicAppCodeGenerationModule.cs
```
