# XiHan.BasicApp 前端审计与优化报告

> 审计范围: `frontend/src` 全量 — API 模块、视图页面、组件、Store、路由、常量
> 审计日期: 2026/05/12
> 后端参考: 40→24 AppService 合并后

---

## 一、严重问题 (CRITICAL — 8项)

### C1. 参数配置页无 CRUD — 只读列表
**文件**: `views/platform/config/index.vue`
**问题**: 只有查询过滤+表格，无新增/编辑/删除/状态切换操作列。API `configManagementApi` 已提供 CRUD 能力，UI 未接入。
**优化**: 添加新增/编辑 Modal、操作列（编辑+状态切换+删除）、详情抽屉。

### C2. 字典管理页无 CRUD — 只读查看
**文件**: `views/platform/dict/index.vue`
**问题**: 仅查询+查看字典项，无新增/编辑/删除字典或字典项的能力。
**优化**: 添加字典 CRUD Modal + 字典项新增/编辑 Modal。

### C3. 租户详情"成员"Tab 为占位符
**文件**: `views/platform/tenant/index.vue` (行540-542)
**问题**: 成员 Tab 硬编码 `<NEmpty description="加载中…" />`，未加载真实数据。
**优化**: 调用 `tenantManagementApi.members.page()` 加载真实成员列表。

### C4. 缺失差异日志页面
**API 存在**: `api/modules/audit/diff-log.ts`，`logManagementApi.diff` 已导出。
**页面缺失**: `views/log/diff/index.vue` 不存在。其余5种日志类型均有页面。
**优化**: 参照 `views/log/operation/index.vue` 创建 diff-log 页面。

### C5. 废弃 API 调用 — 16个从属服务已合并到父服务
**问题**: 后端已将 16 个从属服务合并到 4 个父聚合根服务（标记 `[Obsolete]`），前端仍在调用旧 API 端点：

| 前端 API 模块 | 调用废弃服务 | 应改为 |
|--------------|-------------|--------|
| `user-role.ts` | UserRoleAppService | UserAppService.xxxUserRole |
| `user-permission.ts` | UserPermissionAppService | UserAppService.xxxUserPermission |
| `user-data-scope.ts` | UserDataScopeAppService | UserAppService.xxxUserDataScope |
| `user-session.ts` | UserSessionAppService | UserAppService.xxxUserSession |
| `role-permission.ts` | RolePermissionAppService | RoleAppService.xxxRolePermission |
| `role-data-scope.ts` | RoleDataScopeAppService | RoleAppService.xxxRoleDataScope |
| `role-hierarchy.ts` | RoleHierarchyAppService | RoleAppService.xxxRoleHierarchy |
| `resource.ts` | ResourceAppService | PermissionAppService.xxxResource |
| `operation.ts` | OperationAppService | PermissionAppService.xxxOperation |
| `permission-condition.ts` | PermissionConditionAppService | PermissionAppService.xxxPermissionCondition |
| `permission-delegation.ts` | PermissionDelegationAppService | PermissionAppService.xxxPermissionDelegation |
| `permission-request.ts` | PermissionRequestAppService | PermissionAppService.xxxPermissionRequest |
| `tenant-member.ts` | TenantMemberAppService | TenantAppService.xxxTenantMember |
| `tenant-edition-permission.ts` | TenantEditionPermissionAppService | TenantEditionAppService.xxxEditionPermission |

**优化**: 更新所有 API 客户端的 DynamicApi 名称指向父服务。

### C6. 仪表盘使用跨模块 API
**文件**: `views/workbench/dashboard/index.vue`
**问题**: 直接调用 `serverManagementApi.getCpuInfo()` 等 platform 层 API。
**优化**: 添加 `workbenchApi.serverMetrics()` 聚合端点，避免跨层调用。

### C7. 枚举选项重复定义导致漂移
**问题**: `constants/business.ts` 和各个 Vue 页面中存在多份枚举选项定义，标签不一致：
- `DEPARTMENT_TYPE_OPTIONS` (16项) vs `deptTypeOptions` (15项) — 标签不同
- `CONFIG_TYPE_OPTIONS` (3项) vs `configTypeOptions` (4项) — 数量不同
- `DEVICE_TYPE_OPTIONS` 在三处定义标签各不相同

**优化**: 删除页面内硬编码，统一从 `~/constants` 导入。

### C8. 仪表盘硬编码模拟数据
**文件**: `views/workbench/dashboard/index.vue` (行106-111)
**问题**: `recentActivities` 是写死的4条字符串，应从 API 获取。

---

## 二、设计问题 (WARNING — 8项)

### W1. 全局枚举无后端同步机制
**问题**: 22个页面全部硬编码 `const xxxOptions = [...]`，后端37个枚举文件无自动同步。
**优化**: 后端暴露 `/api/basicapp/enums` 端点，前端创建 `useEnum()` composable。

### W2. 菜单管理页缺详情抽屉
**优化**: 参照 role/permission 页面添加 NDrawer 详情视图（显示关联权限、子菜单树）。

### W3. 机构管理页缺详情抽屉
**优化**: 参照 role/permission 页面添加 NDrawer 详情视图（显示子部门、部门成员）。

### W4. 状态切换确认不一致
- User: 无确认直接切换
- Role/Permission: 有 NPopconfirm
- Org: 无确认文本按钮

**优化**: 统一所有状态切换使用 NPopconfirm。

### W5. 语言字段为自由文本输入
**文件**: `views/system/user/index.vue` (行837)
**问题**: 语言用 NInput 自由输入，应改为 NSelect 带标准 locale 列表。

### W6. 远程搜索无分页
**文件**: `views/system/permission/index.vue` (行469-501)
**问题**: `loadResourceOptions/loadOperationOptions` 硬编码 `limit:50`，超过50条无法搜索。

### W7. 缓存管理页空结果无状态提示
**问题**: `cacheKeys` 为空时无 NEmpty 组件。

### W8. 列配置不一致
各管理页展示的元数据字段不同（有的缺 modifiedTime，有的缺 description）。

---

## 三、改进项 (INFO — 5项)

1. 无加载骨架屏（仅仪表盘有 NSkeleton）
2. 无全局错误边界组件
3. 无批量操作（多选+批量删除/启停）
4. 仪表盘快捷入口无权限检查
5. NEmpty 组件均为纯文字，无图示

---

## 四、优化优先级

### P0 — 立即执行
1. C5: 更新 API 客户端指向合并后的父服务（防止后端移除旧服务时崩溃）
2. C4: 创建差异日志页面
3. C3: 租户成员 Tab 接入真实数据
4. C7: 统一枚举选项，消除漂移

### P1 — 功能补全
5. C1: 配置页添加 CRUD
6. C2: 字典页添加 CRUD
7. W2: 菜单页添加详情抽屉
8. W3: 机构页添加详情抽屉
9. W4: 统一状态切换确认模式

### P2 — 体验优化
10. W1: 后端枚举端点 + useEnum composable
11. W5: 语言选择器
12. C8: 仪表盘动态数据
13. 加载骨架屏 / 错误边界
