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

流水推进不经过应用层的“读取当前值 → 计算新值 → 写回”，而是由数据库内的两条条件更新语句完成：先把周期基线单调翻转到当前周期，再在当前周期内累加占用一段连续区间，随后在同一事务内读回本次写入以确定区间边界。因此不存在丢失更新，也没有乐观锁冲突重试。并发请求在规则行的排他锁上排队，由数据库串行化。

推进语句同时把 `RowVersion` 加一，使管理端“加载整实体 → 改字段 → 乐观锁回写”无法把发号期间推进过的流水打回旧值。

周期翻转只接受更严格递增的周期序号，时钟落后的节点不会把规则拉回上一周期重发编号，而是收到明确的时间同步提示并被拒绝。

#### 事务契约

发号必须在数据库事务内完成，这是“推进后读回值等于本次推进结果”的前提。发号**总是**以 `requiresNew` 开启自己的事务并当场提交，语义对齐数据库序列。在调用方事务内调用时，框架会为发号物化一条独立物理连接。由此：

- **编号发出即永久消耗**，不随调用方事务回滚而回收。已经外泄的编号（写日志、打印、下发外部系统）不可能被再次发给另一个业务实体。
- **规则行的锁只在发号期间持有**，不被调用方事务的剩余时长拖住，取号之后再调用外部接口不影响该规则吞吐。
- **不要在已修改过同一条规则行的事务里发号**：发号走另一条连接，会等待调用方自己尚未提交的行锁，而这个等待环有一半在应用线程上，数据库的死锁检测看不到。业务代码正常不会直接改规则行，这条只针对管理端与发号混在同一事务的写法。
- **部署形态无关**：共享库与独立租户库、租户私有规则与平台全局规则，走的是同一条路径、同样独立提交，不存在行为分叉。

#### 空洞

编号空洞是预期行为而非异常：调用方事务回滚、推进成功后写入分配记录失败、相同幂等键并发竞争落败、管理员安全重置时主动跳号，都会产生空洞。业务编号保证唯一与单调分配，不保证连续，下游的对账、审计和按号推算业务量都不应假设编号连续。

#### 空洞的代价与收益

编号独立提交意味着调用方事务失败时编号已经消耗，空洞比“随调用方回滚”的方案更多。换来的是三条更重要的性质：编号不会被回收后重发、规则行不被长事务拖住、所有部署形态行为一致。对业务编号而言唯一性优先于连续性，因此这个取舍是刻意的。

#### 数据变更日志

数据变更日志的变更后镜像由 ORM 用同一份 WHERE 回查得到。周期翻转语句的守卫是「周期序号更小」，执行后必然不再成立，因此翻转的后镜像总是空；流水推进语句只在推进到接近容量上限时后镜像为空，其余情况正常。

这不影响审计完整性：每次推进都会在发号记录表留下一行，含起止流水、周期、发号时间、操作人和幂等键，比一条差异记录信息更全；规则元数据（名称、状态、格式）的变更仍走实体式更新，差异记录完整。

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
