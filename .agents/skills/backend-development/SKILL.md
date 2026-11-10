---
name: xihan-basicapp-backend-development
description: 实现、重构、审查或测试 XiHan.BasicApp 后端（backend/，基于 XiHan.Framework）时使用。覆盖能力包与 Tier、Feature 显式登记、操作与 Expose、权限码与种子、错误与消息目录、仓储与 Queries、平台库与租户库实体、表结构基线与升级脚本、宿主 Provider、快照构建与测试门禁；跨端契约变化同时用 fullstack-contracts。
---

# XiHan.BasicApp 后端开发

用于 `backend/`。开始前读 `references/backend.md`，再读方案 `plans/下一代总体方案.md` 第 5 节里对应的小节、`docs/backend/development.md`，以及目标包的 `<包>Feature.cs`、相邻操作、`Queries/` 与集成测试。

## 架构边界

- 能力包按 Tier 分层（Tier 0 `Contracts → Runtime`，Tier 1 `Identity → Tenancy → Organization → Access`，Tier 2 平台能力，Tier 3 可选模块，`Host` 装配），只能向下依赖，同层之间经 `Contracts` 的接口协作；`TierDependencyTests` 守护。
- 每个包一个 `<包>Feature : IFeature`：手写清单，在 `Configure` 里显式登记仓储、服务、权限目录、页面、消息目录、中间件、作业与 `context.AddGeneratedOperations()`。没有约定扫描、`IScopedDependency` 或拦截器。
- 业务接口是操作：`Operations/<资源 Pascal>/<动词>.cs` 里一个 `[Operation(Id, Expose = …, Permission = …)]` 类实现 `IOperation<TRequest, TResponse>`。不写 Controller 或 Minimal API 业务入口；普通端点只留给文件上传下载、OAuth 重定向、Hub、Webhook。
- `Expose` 每个操作显式写（`XHG004` 为 error）；超出 `Expose.Http` 的必须只读并登记在 `OperationExposureTests`。
- 权限码 `<包>.<资源>.<动作>`，资源段等于操作 ID 的资源段；在 `<包>Permissions.All` 登记 `PermissionDescriptor`（作用侧、`audited`），权限目录、菜单门控与种子都由它派生。授权只在后端，前端隐藏按钮不是安全边界。
- 平台库实体用 `OwnerTenantId`（平台 = `TenantIds.Platform`）分侧，租户库实体用 `TenantScoped*` 基类；不绕过租户过滤与数据范围。
- 失败是值：`Error.Validation / NotFound / Conflict / Denied / Unavailable(<包>Errors.X, <包>Messages.X, MessageArguments.Of(...))`；新消息键同时补 10 个 `Localization/<区域>.json`。不吞异常、不返回伪成功。
- 事务由管道的 Transaction 阶段开；`context.CancellationToken` 一路传下去。要独立于调用事务的读写（失败计数、门禁读库、缓存加载）走 `Runtime.Persistence.StoreScope`。
- 框架问题回 XiHan.Framework 仓库修，BasicApp 只升锁定提交并跟进，不写兼容、不加静默兜底。

## 契约与验证

改了操作、DTO、权限码、页面登记或推送方法，同时加载 `../fullstack-contracts/SKILL.md`，契约导出与前端生成物同一提交。

`XiHan.BasicApp.slnx` 用框架的 NuGet 包（`backend/props/framework.props` 的 `XiHanFrameworkPackageVersion`）；以源码联调框架用 `XiHanFun.Local.slnx`，同级检出要在锁定提交 `XiHanFrameworkCommit` 上，检出前进了就按 `references/backend.md` 的快照做法。提交前：Release 构建 0 警告、架构测试与集成测试全过（快照构建下只有 `FrameworkPinTests` 失败属预期）、`git diff --check`、单行 summary 计数为 0。
