# 数据模型

数据表清单，按业务域分组（`XiHan.BasicApp.Saas` 与 `XiHan.BasicApp.Workflow` 两个模块；代码生成、AI 模块的表不在此列）。想知道某张表归谁管、某个功能落在哪张表，看这页。

实体基类、列约定、软删与索引规范见 [实体基类](./entity)；连接与初始化见 [数据库配置](./database)。

## 命名速查

| 约定 | 值 |
| --- | --- |
| 表名前缀 | `Sys_` |
| 命名风格 | `snake_case` |
| 主键列 | **`Basic_Id`**（不是 `Id`） |
| 多租户列 | `Tenant_Id`，**全局记录 = 0** |
| 软删列 | `Is_Deleted` |
| 分表后缀 | `_{year}{month}{day}` + `SplitType.Month`（按月，形如 `_20260801`） |

## 身份与安全

| 表 | 实体 | 说明 |
| --- | --- | --- |
| `Sys_User` | `SysUser` | 用户主体，`Email` 全平台唯一 |
| `Sys_User_Security` | `SysUserSecurity` | 一对一安全扩展：密码哈希、锁定、MFA、多端策略 |
| `Sys_User_Session` | `SysUserSession` | 会话中心（多端控制、撤销） |
| `Sys_User_Setting` | `SysUserSetting` | 用户 UI 偏好（按场景 + key 存 JSON） |
| `Sys_User_Statistics` | `SysUserStatistics` | 用户统计 |
| `Sys_User_Api_Credential` | `SysUserApiCredential` | 开放接口个人凭证（AppKey + AppSecret 密文，Data Protection 可逆加密） |
| `Sys_External_Login` | `SysExternalLogin` | 第三方身份绑定 |
| `Sys_Password_History` | `SysPasswordHistory` | 历史密码（防重用） |

→ [统一认证](./authentication)

## 权限与授权

| 表 | 实体 | 说明 |
| --- | --- | --- |
| `Sys_Role` / `Sys_Role_Hierarchy` | `SysRole` / `SysRoleHierarchy` | 角色与层级继承（闭包） |
| `Sys_Permission` | `SysPermission` | 权限点 |
| `Sys_Operation` / `Sys_Resource` | `SysOperation` / `SysResource` | 操作字典与资源，**权限由「资源 × 操作」派生** |
| `Sys_Role_Permission` / `Sys_User_Permission` | — | 角色授权 / 用户直授 |
| `Sys_Role_Data_Scope` / `Sys_User_Data_Scope` | — | 自定义数据范围 |
| `Sys_Permission_Condition` | `SysPermissionCondition` | ABAC 属性条件 |
| `Sys_Permission_Delegation` | `SysPermissionDelegation` | 权限委托 |
| `Sys_Permission_Request` | `SysPermissionRequest` | 权限申请 |
| `Sys_Field_Level_Security` | `SysFieldLevelSecurity` | 字段级安全（可读 / 可编辑 / 脱敏策略） |
| `Sys_Constraint_Rule` / `Sys_Constraint_Rule_Item` | — | 约束规则引擎（SSD / DSD / 互斥 / 基数…） |
| `Sys_Session_Role` | `SysSessionRole` | 会话角色映射 |

→ [权限管理](./permission) · [数据权限](./data-permission) · [审批与约束](./approval)

## 组织与租户

| 表 | 实体 | 说明 |
| --- | --- | --- |
| `Sys_Department` / `Sys_Department_Hierarchy` | — | 部门树与**闭包表** |
| `Sys_Position` | `SysPosition` | 岗位字典 |
| `Sys_User_Department` | `SysUserDepartment` | 用户多部门归属（主部门、岗位、工号） |
| `Sys_User_Role` | `SysUserRole` | 用户角色（支持生效期） |
| `Sys_Tenant` | `SysTenant` | 租户 |
| `Sys_Tenant_User` | `SysTenantUser` | 租户成员关系 |
| `Sys_Tenant_Edition` / `Sys_Tenant_Edition_Permission` | — | 版本套餐与可用权限白名单 |

→ [组织架构](./organization) · [多租户 SaaS](./multi-tenancy)

## 系统设置

| 表 | 实体 | 说明 |
| --- | --- | --- |
| `Sys_Menu` | `SysMenu` | 菜单（`PageRegistry` 的落库形态） |
| `Sys_Dict` / `Sys_Dict_Item` | — | 数据字典 |
| `Sys_Config` | `SysConfig` | 参数配置 |
| `Sys_Numbering_Rule` / `Sys_Numbering_Allocation` | — | 业务编号规则与分配记录 |
| `Sys_Version` | `SysVersion` | 版本管理 |
| `Sys_Migration_History` | `SysMigrationHistory` | 升级迁移历史 |

→ [系统设置](./settings)

## 消息与通知

| 表 | 实体 | 说明 |
| --- | --- | --- |
| `Sys_Notification` / `Sys_User_Notification` | — | 通知公告与逐用户接收状态 |
| `Sys_User_Notification_Preference` | — | 通知偏好（渠道 × 类型） |
| `Sys_Message_Template` | `SysMessageTemplate` | 消息模板（Scriban） |
| `Sys_Email` / `Sys_Email_Config` | — | 邮件记录与网关配置 |
| `Sys_Sms` / `Sys_Sms_Config` | — | 短信记录与网关配置 |
| `Sys_Bot_Config` | `SysBotConfig` | 钉钉 / 飞书 / 企业微信 Webhook 机器人 |
| `Sys_Telegram_Bot` | `SysTelegramBot` | 多实例 Telegram Bot |
| `Sys_Chat_Conversation` / `_Member` / `Sys_Chat_Message` / `_Reaction` | — | 在线聊天 |

→ [消息通知](./messaging) · [即时通讯](./realtime)

## 文件与任务

| 表 | 实体 | 说明 |
| --- | --- | --- |
| `Sys_File` / `Sys_File_Storage` | — | 文件元数据与存储位置（一对多） |
| `Sys_Storage_Config` | `SysStorageConfig` | 存储后端配置（本地 / S3 / OSS / COS / MinIO） |
| `Sys_Export_Task` | `SysExportTask` | 异步导出任务 |
| `Sys_Import_History` | `SysImportHistory` | 导入历史 |
| `Sys_Task` | `SysTask` | 定时任务定义 |

→ [文件与存储](./file) · [定时任务](./scheduling)

## 开放能力与审批

| 表 | 实体 | 说明 |
| --- | --- | --- |
| `Sys_OAuth_App` / `Sys_OAuth_Code` / `Sys_OAuth_Token` | — | 作为 OAuth2 / OIDC **服务端**的应用、授权码与令牌 |
| `Sys_Review` | `SysReview` | 审批 / 审查单 |

→ [开放接口](./open-api) · [审批与约束](./approval)

## 工作流

`XiHan.BasicApp.Workflow` 模块的四张表：

| 表 | 实体 | 说明 |
| --- | --- | --- |
| `Sys_Workflow_Definition` | `SysWorkflowDefinition` | 流程定义 |
| `Sys_Workflow_Instance` | `SysWorkflowInstance` | 流程实例 |
| `Sys_Workflow_Node_Instance` | `SysWorkflowNodeInstance` | 节点实例 |
| `Sys_Workflow_Bookmark` | `SysWorkflowBookmark` | 书签（挂起点） |

→ [工作流](./workflow)

## 日志（均按月分表）

| 表 | 说明 |
| --- | --- |
| `Sys_Access_Log` | 访问日志 |
| `Sys_OpenApi_Log` | 开放接口日志 |
| `Sys_Operation_Log` | 操作日志 |
| `Sys_Login_Log` | 登录日志 |
| `Sys_Exception_Log` | 异常日志 |
| `Sys_Diff_Log` | 数据变更日志（需开 `EnableDiffLog`） |
| `Sys_Permission_Change_Log` | 权限变更日志 |
| `Sys_Review_Log` | 审批日志 |
| `Sys_Task_Log` | 任务执行日志 |
| `Sys_Telegram_Message` | Telegram 出站审计 |

::: warning 分表查询要带时间范围
物理表名形如 `Sys_Access_Log_20260801`，要走 SqlSugar 的分表 API 按时间定位，不能当普通表直接查。
:::

→ [日志审计](./logging)

## 相关页面

- [实体基类](./entity)：基类选型与列约定
- [数据库配置](./database)：连接、初始化、分表
- [框架简介](./introduction)：模块与分层
