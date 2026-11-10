来源：https://basicapp.docs.xihanfun.com/backend/data-model

# 数据模型

本页说明各包实体落在平台库还是租户库、各自实现哪些协议接口，以及索引与唯一性规则，适用于需要了解表结构归属的后端开发者。实体基类见[实体基类](./entity)。

## 落点规则

数据模型按「库决定协议」划分：实体落在哪个库，决定它用哪组基类、实现哪些接口。

| | 平台库实体 | 租户库实体 |
| --- | --- | --- |
| 基类 | `AppEntity` / `AppRecord` / `AppAuditedEntity` / `AppAggregateRoot` | `TenantScopedEntity` / `TenantScopedRecord` / `TenantScopedAuditedEntity` / `TenantScopedAggregateRoot` |
| `IMultiTenant` | 不实现 | 实现，框架仓储按当前租户严格过滤 |
| 归属 | 需要分侧时实现 `IOwnedByTenant`，列 `OwnerTenantId`（平台为 `platform`）；账号用 `HomeTenantId` | `TenantId`；平台一侧的数据以 `platform` 为租户落在同一张表 |
| 连接 | 永远是默认连接（平台库），独立库的租户也读写平台库里的这些表 | 租户有独立库时落 `shard` 连接，否则与平台库同库、按 `tenant_id` 过滤 |
| 例子 | 用户、会话、租户、角色与授权、菜单、参数、字典、模板、各类配置 | 部门、岗位、文件、通知与收件箱、日志、聊天、知识文档 |

框架仓储要求租户库实体的调用带租户，平台一侧（`platform`）的行经 Runtime 的 `SideRows` 按侧读写。平台扫各租户库的后台工作（日志写入、保留清理、独立库初始化与升级）在自建作用域里经框架 `TenantShardScope` 按租户绑定分片，不在同一个上下文里切换租户。

## 平台库实体

### 身份（Identity）

| 实体 | 说明 |
| --- | --- |
| `SysUser` | 账号：`HomeTenantId` 为注册地租户（平台账号为 `platform`），状态、是否系统内置账号、资料与头像 |
| `SysUserSecurity` | 账号安全：密码散列、失败计数、锁定期、双因素（密钥加密存储）、开放接口签名设置、登录策略 |
| `SysUserSession` | 会话：ID 即令牌的 `sid`，`ActiveTenantId` 为会话当前所在的租户（平台为空）；刷新令牌散列、客户端信息、登录地点、锁屏、模仿发起人 |
| `SysPasswordHistory` | 密码历史（不可复用的深度见 `BasicApp:Auth:PasswordHistoryDepth`） |
| `SysUserSetting` | 用户设置（用户 × 场景 × 键，值是 JSON）：偏好、收藏、工作台布局 |
| `SysUserNotificationPreference` | 通知偏好 |
| `SysUserExternalLogin` | 第三方账号绑定 |
| `SysUserApiCredential` | 开放接口凭证（密钥加密存储） |

### 租户（Tenancy）

| 实体 | 说明 |
| --- | --- |
| `SysTenant` | 租户：目录键 `TenantId`、名称、状态、到期、管理员（即所有者）、套餐、配额、隔离方式与独立库状态；与框架 `TenantRecord` 同事务同步 |
| `SysTenantMember` | 租户成员与运维支持成员，带生效与到期 |
| `SysEdition` / `SysEditionGrant` | 套餐（价格、计费周期、用户与存储配额）与权限白名单 |

### 授权（Access）

除 `SysMenu` 外都实现 `IOwnedByTenant`，按侧分开。

| 实体 | 说明 |
| --- | --- |
| `SysRole` | 角色，带数据范围 |
| `SysUserRole` | 角色指派，带生效与到期 |
| `SysRoleInheritance` | 角色继承（只存直接边） |
| `SysPermissionRule` | Deny 与带有效期或原因的授予；普通授予只写框架的 `PermissionGrantRecord` |
| `SysPermissionDelegation` | 限时委托 |
| `SysPermissionCondition` | 挂在授权绑定上的 ABAC 条件 |
| `SysRoleDataScope` · `SysUserDataScope` · `SysUserDataScopeDepartment` | 角色的自定义部门、用户级数据范围覆盖及其部门 |
| `SysFieldSecurityRule` | 字段安全规则 |
| `SysAccessPolicy` · `SysAccessPolicyCondition` | 访问策略及其条件 |
| `SysConstraintRule` · `SysConstraintRuleItem` | 约束规则及其规则项 |
| `SysSessionRole` | 会话激活的角色 |
| `SysMenu` | 菜单：两侧共用一棵树，页面节点由页面登记派生，另有运营建的目录与外链 |

### 平台能力与可选模块

| 包 | 实体 |
| --- | --- |
| Settings | `SysConfig`、`SysDict`、`SysDictItem`、`SysNumberingRule`、`SysNumberingAllocation`（均 `IOwnedByTenant`） |
| Notification | `SysMessageTemplate`、`SysEmail`、`SysSms`、`SysBotMessage`、`SysEmailConfig`、`SysSmsConfig`、`SysBotConfig`（均 `IOwnedByTenant`）；`SysTelegramBot`、`SysTelegramMessage`（只在平台） |
| Files | `SysStorageConfig`（`IOwnedByTenant`，密钥加密存储） |
| OpenPlatform | `SysOAuthApp`、`SysOAuthCode`、`SysOAuthToken`、`SysOAuthConsent`、`SysOAuthSigningKey` |
| Printing | `SysPrintTemplate`（`IOwnedByTenant`） |
| AI | `SysAiProvider`、`SysAiPrompt`、`SysAiAssistant`（只在平台） |
| CodeGeneration | `SysCodeGenDataSource`、`SysCodeGenTable`、`SysCodeGenTableColumn`、`SysCodeGenTemplate`、`SysCodeGenHistory`（只在平台） |
| Runtime | `SysVersion`、`SysMigrationHistory`：结构版本台账与升级脚本记录，平台库与每个独立库各一份 |

## 租户库实体

| 包 | 实体 | 说明 |
| --- | --- | --- |
| Organization | `SysDepartment`、`SysDepartmentHierarchy`、`SysPosition`、`SysUserDepartment` | 部门（闭包表维护层级）、岗位、部门成员（聚合根，发出领域事件） |
| Identity | `SysUserStatistics` | 用户使用统计，在注册地租户 |
| Access | `SysPermissionRequest` | 授权申请 |
| Files | `SysFile`、`SysFileStorage`、`SysFileUpload`、`SysExportTask`、`SysImportHistory` | 文件、副本、分片上传会话、导出任务、导入历史 |
| Notification | `SysNotification`、`SysUserNotification` | 通知与按人投递 |
| Auditing | `SysAccessLog`、`SysLoginLog`、`SysExceptionLog`、`SysDiffLog`、`SysPermissionChangeLog`、`SysConditionHit`、`SysOpenApiLog` | 七类日志，按月分表（`[SplitTable(SplitType.Month)]`），只追加 |
| Workflow | `SysWorkflowTaskRecord`、`SysWorkflowMessageReceipt` | 审批记录（办理、转办、加签、抄送）与已处理的工作流事件回执 |
| Chat | `SysChatConversation`、`SysChatConversationMember`、`SysChatMessage`、`SysChatMessageReaction` | 会话、成员、消息、表情回应 |
| AI | `SysKnowledgeDocument` | 知识文档 |

操作日志没有应用表：它就是框架的操作审计 `audit_record`（经 `Auditing.Database` 落库），Auditing 包只提供查询面。

按月分表的日志不在基线脚本里，写入时由框架按月建表。

## 框架 Provider 的表

基线 `Schema/6.0.0/0001-baseline.sql` 同时包含框架 Provider 的表，由同一套升级器管理：

| 表 | 来源 |
| --- | --- |
| `tenant_record` | `MultiTenancy.Database`：租户目录与分片属性 |
| `permission_grant_record` | `Security.Database`：权限授予 |
| `audit_record` | `Auditing.Database`：操作审计（即操作日志） |
| `job_entity`、`trigger_entity`、`job_run_entity`、`scheduler_lease_entity`、`scheduler_node_entity` | `Scheduling.Database`：作业、触发器、运行记录、租约与节点 |
| `workflow_*_record`、`platform_workflow_*_record`、`workflow_lock_record` | `Workflow.Database`：流程定义、实例、节点实例与书签（租户与平台各一组）及租约 |
| `message_translation` | `Localization.Database`：运营覆盖的译文 |
| `outbox_message` | 事务发件箱 |

## 索引与唯一性

编码、键名这类唯一性由操作在写入前检查并返回本包的 `CONFLICT` 错误码（如参数键在本侧唯一），数据库再用唯一索引兜住并发写入：

- 软删除实体用部分唯一索引，只约束未删除的行（`ux_<表>_<列>… WHERE is_deleted = false`）：同一个编码删了再建、再删都不冲突。实体上写 `[ActiveUniqueIndex(nameof(…))]`，开发库由 Runtime 在建表之后补建，独立库在初始化时补建，生产库随基线脚本建出；只支持 PostgreSQL 与 SQLite。
- 不软删除的实体（关联表、只增的记录）用唯一的 `[SugarIndex]`；外键查找与列表的筛选、排序用普通 `[SugarIndex]`（`ix_<表>_<列>`）。按月分表的日志用 `{split_table}` 占位，每张月表各带一份。
- 结构测试（`EntityIndexTests`）守护：软删除实体不写唯一的 `[SugarIndex]`，部分唯一索引只用于软删除实体，列都是实体的属性，索引名不超过 PostgreSQL 的 63 字符（过长时写 `Name` 给一个短名）且不重名。

跨归属的规则（如字典码平台与租户不可重复）索引表达不了，只靠操作里的检查。并发写入撞上唯一 `[SugarIndex]` 时，框架仓储核对后按唯一键重复返回 `PERSISTENCE_CONFLICT_003`；撞上 `[ActiveUniqueIndex]` 建的部分唯一索引时框架识别不了，第二个写入方拿到的是数据库错误。

## 下一步

- [数据库配置](./database) · [多租户 SaaS](./multi-tenancy) · [表结构升级](./upgrade)
