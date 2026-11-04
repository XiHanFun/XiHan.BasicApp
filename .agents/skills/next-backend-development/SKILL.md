---
name: xihan-basicapp-next-backend-development
description: 在 next 分支实现、重构、审查或测试 XiHan.BasicApp 6.0 后端（backend-next，基于 XiHan.Framework 5）时使用。覆盖能力包与 Tier、Feature 显式登记、操作与 Expose、权限码与种子、错误与消息目录、仓储与 Queries、平台库与租户库实体、6.0.0 基线、宿主 Provider、快照构建与测试门禁；4.x 的 backend/ 用 backend-development，跨端契约同时用 next-fullstack-contracts。
---

# XiHan.BasicApp 6.0 后端开发

只用于 `next` 分支的 `backend-next/`。4.x 的 Dynamic API、`[UnitOfWork]`、`[PermissionAuthorize]`、`TenantId = 0` 写法在这里一律不适用。

开始前读 `references/backend.md`，再读方案 `nextdocs/下一代总体方案.md` 第 5 节里对应的小节、`docs/backend/development.md`，以及目标包的 `<包>Feature.cs`、相邻操作、`Queries/` 与集成测试。

## 架构边界

- 能力包按 Tier 分层（Tier 0 `Contracts → Runtime`，Tier 1 `Identity → Tenancy → Organization → Access`，Tier 2 平台能力，Tier 3 可选模块，`Host` 装配），只能向下依赖，同层之间经 `Contracts` 的接口协作；`TierDependencyTests` 守护。
- 每个包一个 `<包>Feature : IFeature`：手写清单，在 `Configure` 里显式登记仓储、服务、权限目录、页面、消息目录、中间件、作业与 `context.AddGeneratedOperations()`。没有约定扫描、`IScopedDependency` 或拦截器。
- 业务接口是操作：`Operations/<资源 Pascal>/<动词>.cs` 里一个 `[Operation(Id, Expose = …, Permission = …)]` 类实现 `IOperation<TRequest, TResponse>`。不写 Controller、Dynamic API 或 Minimal API 业务入口；普通端点只留给文件上传下载、OAuth 重定向、Hub、Webhook。
- `Expose` 每个操作显式写（`XHG004` 为 error）；超出 `Expose.Http` 的必须只读并登记在 `OperationExposureTests`。
- 权限码 `<包>.<资源>.<动作>`，资源段等于操作 ID 的资源段；在 `<包>Permissions.All` 登记 `PermissionDescriptor`（作用侧、`audited`），权限目录、菜单门控与种子都由它派生。
- 失败是值：`Error.Validation / NotFound / Conflict / Denied / Unavailable(<包>Errors.X, <包>Messages.X, MessageArguments.Of(...))`；新消息键同时补 10 个 `Localization/<区域>.json`。
- 事务由管道的 Transaction 阶段开；`context.CancellationToken` 一路传下去。要独立于调用事务的读写（失败计数、门禁读库、缓存加载）走 `Runtime.Persistence.StoreScope`。
- 框架问题回框架仓库修，BasicApp 不写兼容、不加静默兜底。

## 契约与验证

改了操作、DTO、权限码、页面登记或推送方法，同时加载 `../next-fullstack-contracts/SKILL.md`，契约导出与前端生成物同一提交。

构建与测试对着框架锁定提交（`backend-next/props/framework.props` 的 `XiHanFrameworkCommit`）；同级框架检出前进了就按 `references/backend.md` 的快照做法。提交前：Release 构建 0 警告、架构测试与集成测试全过（快照构建下只有 `FrameworkPinTests` 失败属预期）、`git diff --check`、单行 summary 计数为 0。
