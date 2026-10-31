# 数据模型

本页列出各包的实体与落点，适用于需要了解表结构归属的后端开发者。6.0 的数据模型按库决定协议划分：一个实体落在平台库还是租户库，决定了它实现哪些协议接口。实体基类见[实体基类](./entity)。

## 落点规则

| | 平台库实体 | 租户库实体 |
| --- | --- | --- |
| 基类 | `App*` | `TenantScoped*` |
| `IMultiTenant` | 不实现 | 实现，框架仓储严格过滤 |
| 归属 | 需要时用 `OwnerTenantId`（平台为 `platform`）或 `HomeTenantId` | `TenantId`（平台一侧的租户库数据记 `platform`） |
| 连接 | 永远是默认连接（平台库） | 租户有独立库时落 `shard` 连接 |
| 例子 | 用户、会话、租户、角色、授权、菜单、参数、模板 | 部门、岗位、文件、通知投递、八类日志 |

平台扫租户库的后台工作（日志写入、保留清理、独立库初始化与升级）在自建作用域里经框架 `TenantShardScope` 按租户绑定分片，不在同一个上下文里切换租户。

## 平台库实体

### 身份（Identity）

| 实体 | 说明 |
| --- | --- |
| `SysUser` | 用户；`HomeTenantId` 为注册地租户（平台账号为 `platform`），状态 Active / Locked / Disabled |
| `SysUserSecurity` | 账号安全：密码散列、失败计数、锁定、双因素密钥（加密存储） |
| `SysUserSession` | 会话：`sid`、刷新令牌散列、客户端信息、登录地点、锁屏、模仿发起人 |
| `SysPasswordHistory` | 密码历史（不可复用深度 `PasswordHistoryDepth`） |
| `SysUserSetting` | 用户设置（用户 × 场景 × 键，值是 JSON）：偏好、收藏、看板布局 |
| `SysUserNotificationPreference` | 通知偏好 |
| `SysUserExternalLogin` | 第三方账号绑定 |
| `SysUserApiCredential` | 开放接口凭证（密钥加密存储） |

### 租户（Tenancy）

| 实体 | 说明 |
| --- | --- |
| `SysTenant` | 租户业务实体：目录键 `TenantId`、名称、状态、到期、管理员（即所有者）、套餐、配额、隔离方式；与框架 `TenantRecord` 同事务同步 |
| `SysTenantMember` | 租户成员与运维支持成员，带生效 / 到期 |
| `SysEdition` / `SysEditionGrant` | 套餐（价格、计费周期、用户与存储配额）与权限白名单 |

### 授权（Access）

| 实体 | 说明 |
| --- | --- |
| `SysRole` | 角色（`OwnerTenantId` 分平台 / 租户侧），带数据范围 |
| `SysUserRole` | 角色指派，带生效 / 到期 |
| `SysRoleInheritance` | 角色继承（只存直接边） |
| `SysPermissionRule` | Deny 与带有效期或原因的授予（普通授予只写框架的 `PermissionGrantRecord`） |
| `SysPermissionDelegation` | 限时委托 |
| `SysPermissionCondition` | 挂在授权绑定上的 ABAC 条件 |
| `SysRoleDataScope` · `SysUserDataScope` · `SysUserDataScopeDepartment` | 角色与用户级数据范围的自定义部门 |
| `SysFieldSecurityRule` | 字段安全规则 |
| `SysAccessPolicy` · `SysAccessPolicyCondition` | 访问策略 |
| `SysConstraintRule` · `SysConstraintRuleItem` | 约束规则 |
| `SysSessionRole` | 会话激活的角色（DSD） |
| `SysMenu` | 菜单：页面节点来自登记，另有运营目录与外链 |

### 平台能力

| 包 | 实体 |
| --- | --- |
| Settings | `SysConfig`、`SysDict`、`SysDictItem`、`SysNumberingRule`、`SysNumberingAllocation`（均 `IOwnedByTenant`） |
| Notification | `SysMessageTemplate`、`SysEmail`、`SysSms`、`SysEmailConfig`、`SysSmsConfig`、`SysBotConfig`、`SysBotMessage`（`IOwnedByTenant`）；`SysTelegramBot`、`SysTelegramMessage` |
| Files | `SysStorageConfig`（`IOwnedByTenant`，密钥加密） |
| OpenPlatform | `SysOAuthApp`、`SysOAuthCode`、`SysOAuthToken`、`SysOAuthConsent`、`SysOAuthSigningKey` |
| Runtime | `SysVersion`、`SysMigrationHistory`：结构版本台账与升级记录（平台库与每个独立库各一份） |

## 租户库实体

| 包 | 实体 | 说明 |
| --- | --- | --- |
| Organization | `SysDepartment`、`SysDepartmentHierarchy`、`SysPosition`、`SysUserDepartment` | 部门（闭包表维护层级）、岗位、部门成员 |
| Identity | `SysUserStatistics` | 用户使用统计 |
| Files | `SysFile`、`SysFileStorage`、`SysFileUpload`、`SysExportTask`、`SysImportHistory` | 文件、副本、分片上传会话、导出任务、导入历史 |
| Notification | `SysNotification`、`SysUserNotification` | 通知与按人投递 |
| Auditing | `SysAccessLog`、`SysLoginLog`、`SysExceptionLog`、`SysDiffLog`、`SysPermissionChangeLog`、`SysConditionHit`、`SysOpenApiLog` | 七类日志按月分表（`[SplitTable(SplitType.Month)]`），只追加；平台一侧的日志租户列记 `platform` |

第八类「操作日志」没有应用表：它即框架的操作审计 `audit_record`（经 `Auditing.Database` 落库），Auditing 包只提供查询面。

## 框架 Provider 的表

6.0.0 基线同时包含框架 Provider 的表，由同一套升级器管理：

| 表 | 来源 |
| --- | --- |
| `tenant_record` | `MultiTenancy.Database`：租户目录与分片属性 |
| `permission_grant_record` | `Security.Database`：权限授予 |
| `audit_record` | `Auditing.Database`：操作审计（即操作日志） |
| `job_entity`、`trigger_entity`、`job_run_entity`、`scheduler_lease_entity`、`scheduler_node_entity` | `Scheduling.Database`：作业、触发器、运行记录、租约与节点 |
| `outbox_message` | 发件箱 |

## 关于唯一性

6.0.0 基线目前只有主键约束，没有唯一索引：编码、键名这类唯一性由操作在写入前检查并返回 `CONFLICT` 错误码（例如参数键在本侧唯一、字典码平台与租户不可重复）。软删除后可以用同样的编码重新创建。

## 下一步

- [数据库配置](./database) · [多租户 SaaS](./multi-tenancy) · [表结构升级](./upgrade)
