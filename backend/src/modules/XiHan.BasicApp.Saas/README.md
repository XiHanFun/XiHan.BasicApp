# XiHan.BasicApp.Saas

## 概述

`XiHan.BasicApp.Saas` 提供 B2B SaaS 场景的 RBAC、轻量 ABAC、多租户基础设施及系统配置能力，并按照现有 DDD 分层组织应用服务、领域服务、实体和仓储。

## 核心能力

- 用户、角色、权限、菜单、租户、配置和字典等基础领域模型。
- 应用服务层的权限管理、系统配置与审计能力。
- 支持共享数据库字段隔离和租户独立数据库。
- 业务编号（Numbering）规则管理、并发安全发号、幂等重放及永久分配审计。

## 业务编号

### 规则作用域

业务编号以 `TenantId` 区分归属：

- `TenantId = 0`：平台全局规则，存储并发号于平台库。
- `TenantId > 0`：租户私有规则，存储并发号于当前租户库。
- 单体应用或平台上下文没有当前租户，按全局规则工作，不需要额外建立租户。

`NumberingScope.Auto` 在租户请求中先查租户私有规则；没有同编码规则时，切换平台上下文查找已启用且允许租户使用的全局规则。`Tenant` 和 `Global` 分别强制使用对应作用域。独立数据库租户回退全局规则时，会先通过 `ICurrentTenant.Change(null)` 切换到平台库，再开启独立事务。

### 格式与周期

编号由所有非空段按分隔符拼接，例如 `ORD-20260727-0001`。支持无日期、`yyyy`、`yyyyMM`、`yyyyMMdd`、`yyMM`、`yyMMdd`、`MMdd`，以及不重置、按日、按月、按年重置。自动重置与日期格式必须能安全区分周期，规则时区默认使用 `UTC`。

管理端时区下拉由后端运行环境的 `TimeZoneInfo.GetSystemTimeZones()` 目录提供，不使用浏览器自行推测的时区集合。Windows 原生时区会先归一为可跨 Windows/Linux 保存的 IANA 标识；无法安全映射到 IANA 的已废弃 Windows 项，以及 Unix tzdata 中尚未进入 .NET Windows 映射表的新 IANA 项，都不会进入新建规则下拉。解析时仍兼容当前平台可直接解析或能够通过系统映射表转换的历史 Windows 标识。这样前端展示的每个选项都能被当前服务实例解析并作为可移植规则保存；自动化测试会遍历全部选项，校验 IANA 标识、冬夏时刻换算、周期键、日期段和格式预览结果。

起始流水固定为 `1`，步长固定为 `1`，流水位数为 `1～18`。API 中的流水边界使用字符串传输，避免 JavaScript 对 18 位整数产生精度损失。

### 幂等、并发与空洞

每次发号必须提供幂等键。幂等唯一范围是“实际规则 + 请求租户 + 幂等键”；相同键和相同参数会从永久分配记录重建结果，相同键但参数不同会返回幂等冲突。批量请求最多生成 1000 个连续编号。

一次发号在同一独立事务中完成幂等检查、规则 `RowVersion` 更新和永久分配记录插入。进程内按“规则所属租户 + 规则 ID”使用异步锁降低竞争，跨节点正确性由数据库乐观锁和唯一索引保证；乐观锁冲突最多重试 5 次并随机短退避。

调用方事务后续失败、主动跳号或安全重置都可能形成空洞，因此业务编号保证唯一和单调分配，不保证无空洞。

### 格式冻结与安全重置

规则首次成功发号后，前缀、分隔符、日期格式、流水位数、重置周期和时区永久冻结；名称、备注、状态及全局规则的租户开放开关仍可修改。已经发号的规则不能删除，只能停用。

安全重置必须填写原因。允许把下一流水值前移以主动留出空洞；当前周期已经发号时，不允许回退到可能重复的区间。全局规则还必须输入规则编码二次确认，并同时满足平台运维上下文和 `saas:numbering:global-manage` 权限。

### DI 调用示例

业务代码通过构造函数注入 `INumberGenerator`。首版不提供静态 Helper 或全局 Service Locator，避免绕过租户上下文、事务和测试边界。

```csharp
using XiHan.BasicApp.Saas.Application.Contracts;
using XiHan.BasicApp.Saas.Domain.Enums;

public sealed class OrderNumberService(INumberGenerator numberGenerator)
{
    public async Task<string> CreateNumberAsync(long orderId, CancellationToken cancellationToken)
    {
        var result = await numberGenerator.GenerateAsync(
            new NumberGenerateRequest(
                RuleCode: "ORDER",
                Scope: NumberingScope.Auto,
                IdempotencyKey: $"order:{orderId}",
                BusinessType: "Order",
                BusinessId: orderId.ToString()),
            cancellationToken);

        return result.Numbers[0];
    }
}
```

真实发号也可调用受 `saas:numbering:generate` 保护的 Dynamic API。管理页面只提供规则管理、格式预览、安全重置和发号记录查看，不提供真实发号按钮。

### 数据库发布

运行时主路径为 SqlSugar CodeFirst。PostgreSQL、MySQL、SQL Server、SQLite 和 Oracle 的正向/回滚参考脚本位于 `backend/scripts/database/numbering/`，不会改变现有应用版本号或自动升级脚本的执行语义。

## 架构与职责

- `Application`：应用服务、DTO、查询、映射与 Dynamic API。
- `Domain`：领域实体、领域服务、规则不变量与仓储契约。
- `Infrastructure`：SqlSugar 仓储和外部基础设施适配。
- `Seeders`：权限与资源初始化数据。
- `Extensions`：模块内服务注册与装配。

## 依赖关系

- `XiHanBasicAppCoreModule`
- `XiHanAuthenticationModule`
- `XiHanAuthorizationModule`

## 配置与约定

- 数据库配置：`XiHan:Data:SqlSugarCore`
- 应用服务实现 `IApplicationService` 以暴露为 Dynamic API。
- 领域模型遵循聚合根、租户上下文和审计约定。

## 使用方式

```csharp
[DependsOn(typeof(XiHanBasicAppRbacModule))]
public class MyModule : XiHanModule
{
}
```
