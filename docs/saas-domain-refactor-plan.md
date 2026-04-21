# SaaS Domain Refactor Plan

## Background

`XiHan.BasicApp.Saas` 的 `Domain\Entities` 已完成重构，但 Application、Infrastructure、Frontend 仍大量沿用旧模型。
本次改造以 `backend/src/modules/XiHan.BasicApp.Saas/Domain/Entities` 为唯一事实源，完全按新实体语义重建，不做历史兼容层。

## Code Facts

1. `SysUser` 已明确区分“主归属租户”与“可访问租户集合”，而现有应用层和前端仍默认 `User == Tenant` 的旧模型。
2. `SysRole` 已引入 `RoleHierarchy`、`RoleDataScope`、`IsGlobal`、显式 `DataScope` 语义，现有前后端只覆盖了其中一部分。
3. `SysPermission`、`SysMenu` 已拆分为“权限原子点”和“纯 UI 菜单结构”，现有前端仍把菜单编码混用为权限表达。
4. `SysDepartment` 已要求树结构环路校验和闭包表语义，现有实现只做了普通树形 CRUD。
5. `SysConstraintRule` 与 `SysFieldLevelSecurity` 已成为正式领域对象，但现有契约、查询、前端管理能力不完整。
6. 现有前端页面和 API 类型中仍混用旧字段，如 `deptId`、`PermissionCode`、`Status`/`TenantStatus`、`RoleType`/`DataScope` 的旧数值假设。
7. 当前前端 `pnpm type-check` 还有一批基础包错误，会影响 SaaS 模块的验证闭环，需要在后续质量门禁阶段统一收敛。

## Goals

1. 以后端实体和注释为准，重建 SaaS 模块的前后端契约、服务、查询和页面。
2. 严格执行多租户隔离、RBAC 为主、轻量 ABAC 补充、FLS 出口生效、审计可追踪。
3. 每一批改动必须可验证、可回滚、可独立提交。

## Non-Goals

1. 不保留旧字段兼容层。
2. 不围绕旧 API 形状做适配性补丁。
3. 不在 Controller/UI 层继续堆积业务规则。

## Batches

| Batch | 标题 | 范围 | 完成标准 | 风险重点 |
|------|------|------|----------|----------|
| Batch-01 | 方案与第一批契约基线 | 方案文档、`User/Tenant/Menu/ConstraintRule` DTO 与前端 API、受影响页面 | 建立可追踪重构面板；首批核心契约不再依赖旧字段语义 | 字段命名漂移、枚举值误配、前端归一化逻辑失真 |
| Batch-02 | 第二批契约基线 | `Role/Permission/Department` DTO、前端 API、枚举映射 | RBAC 主链剩余契约按实体语义统一 | 旧数值语义残留、树形字段含义不一致 |
| Batch-03 | 用户/租户主链重建 | `User`、`Tenant`、`Auth`、`UserSession`、相关页面 | 登录/会话支持目标租户上下文，用户维护支持成员租户、角色、部门、权限 | 越权访问、租户上下文错绑、删除/禁用状态遗漏 |
| Batch-04 | RBAC 主链重建 | `Role`、`Permission`、`Menu`、`Department`、查询服务、系统页 | 角色继承、菜单绑定权限、数据范围、部门树闭包语义前后端一致 | 菜单权限错绑、数据范围放大、树结构坏链 |
| Batch-05 | ABAC/FLS 重建 | `ConstraintRule`、`FieldLevelSecurity`、授权出口、页面 | 约束规则、字段脱敏、可编辑性规则能被完整维护和执行 | 脱敏下沉位置错误、规则冲突合并错误 |
| Batch-06 | 前端整体验证与交互重建 | `frontend/src/views/system/**`、路由与交互 | 系统管理页面按新模型可用，页面不再依赖旧字段 | 页面状态错乱、批量编辑丢字段、树与列表联动错误 |
| Batch-07 | 质量门禁与收尾 | 后端构建、前端 type-check、回归清单、文档 | 关键构建通过，输出验证结果、风险余项、回滚建议 | 全局类型错误挡住 SaaS 验证、未覆盖的边界场景 |

## Requirement Table

| ID | 标题 | 范围 | 完成标准 | 依赖 |
|----|------|------|----------|------|
| REQ-001 | 重建核心契约层 | Dto、API modules、枚举映射 | DTO 与前端模型准确反映实体含义 | — |
| REQ-002 | 重建用户与租户主链 | Auth/User/Tenant/UserSession | 用户和租户上下文语义正确落地 | REQ-001 |
| REQ-003 | 重建 RBAC 主链 | Role/Permission/Menu/Department | 授权链、菜单树、数据范围统一 | REQ-001 |
| REQ-004 | 重建轻量 ABAC 与 FLS | ConstraintRule/FLS/授权出口 | ABAC 与字段级安全具备完整能力 | REQ-001, REQ-003 |
| REQ-005 | 重建前端系统页 | `frontend/src/views/system/**` | 管理页面按新模型完成联调 | REQ-001, REQ-002, REQ-003, REQ-004 |
| REQ-006 | 完成质量门禁与交付 | 构建、类型检查、回归、文档 | 可验证、可回滚、结果可追踪 | REQ-001, REQ-002, REQ-003, REQ-004, REQ-005 |

## Execution Rules

1. 每个 Batch 完成后立即更新本文件状态。
2. 每个 Batch 完成后执行本地验证，并记录结果。
3. 每个 Batch 完成后执行一次本地 `git commit`，不 push。
4. 若出现跨 Batch 的阻塞问题，先在当前 Batch 标记风险，再拆出后续修复，不把无关改动混入同一提交。

## Batch Status

- [x] Batch-01 方案与第一批契约基线
- [ ] Batch-02 第二批契约基线
- [ ] Batch-03 用户/租户主链重建
- [ ] Batch-04 RBAC 主链重建
- [ ] Batch-05 ABAC/FLS 重建
- [ ] Batch-06 前端系统页重建与联调
- [ ] Batch-07 质量门禁与收尾

## Validation Template

| Batch | Validation | Result | Notes |
|------|------------|--------|-------|
| Batch-01 | `dotnet build` / `pnpm type-check` / 契约自检 | Partial | `dotnet build` 当前只返回“生成失败，0 错误”；`pnpm type-check` 仍被全局历史错误阻塞，但本批引入的 `tenant/constraint-rule` 页面类型错误已收口 |
| Batch-02 | `dotnet build` / 契约自检 | Pending |  |
| Batch-03 | `dotnet build` / 认证链自检 | Pending |  |
| Batch-04 | `dotnet build` / 授权链自检 | Pending |  |
| Batch-05 | `dotnet build` / 安全出口自检 | Pending |  |
| Batch-06 | `pnpm type-check` / 页面联调自检 | Pending |  |
| Batch-07 | 汇总验证 | Pending |  |
