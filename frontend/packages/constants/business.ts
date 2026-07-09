/**
 * 下拉选项与后端 API 字符串枚举对齐（Flags 类枚举除外）
 */
import {
  AuditResult,
  AuditStatus,
  ConditionOperator,
  ConfigDataType,
  ConfigType,
  ConstraintType,
  DataPermissionScope,
  DelegationStatus,
  DepartmentType,
  DeviceType,
  EmailStatus,
  EmailType,
  EnableStatus,
  FieldMaskStrategy,
  FieldSecurityTargetType,
  FileStatus,
  FileType,
  HttpMethodType,
  MenuType,
  NotificationStatus,
  NotificationType,
  OAuthAppType,
  OperationCategory,
  OperationTypeCode,
  PermissionAction,
  PermissionChangeType,
  PermissionRequestStatus,
  PermissionType,
  ResourceAccessLevel,
  ResourceType,
  RoleType,
  RunTaskStatus,
  SmsStatus,
  SmsType,
  StatisticsPeriod,
  TenantConfigStatus,
  TenantDatabaseType,
  TenantIsolationMode,
  TenantMemberInviteStatus,
  TenantMemberType,
  TenantStatus,
  TriggerType,
  TwoFactorMethod,
  UserGender,
  ValidityStatus,
  ViolationAction,
} from '../../src/api'

// ==================== 性别 ====================

export const GENDER_OPTIONS = [
  { label: '未知', value: UserGender.Unknown },
  { label: '男', value: UserGender.Male },
  { label: '女', value: UserGender.Female },
]

// ==================== 通用状态 ====================

export const STATUS_OPTIONS = [
  { label: '启用', value: EnableStatus.Enabled },
  { label: '禁用', value: EnableStatus.Disabled },
]

export const VALIDITY_STATUS_OPTIONS = [
  { label: '无效', value: ValidityStatus.Invalid },
  { label: '有效', value: ValidityStatus.Valid },
]

// ==================== 部门 ====================

export const DEPARTMENT_TYPE_OPTIONS = [
  { label: '集团', value: DepartmentType.Corporation },
  { label: '总部', value: DepartmentType.Headquarters },
  { label: '公司', value: DepartmentType.Company },
  { label: '分公司', value: DepartmentType.Branch },
  { label: '事业部', value: DepartmentType.Division },
  { label: '中心', value: DepartmentType.Center },
  { label: '部门', value: DepartmentType.Department },
  { label: '科室', value: DepartmentType.Section },
  { label: '团队', value: DepartmentType.Team },
  { label: '小组', value: DepartmentType.Group },
  { label: '项目组', value: DepartmentType.Project },
  { label: '工作组', value: DepartmentType.Workgroup },
  { label: '虚拟组织', value: DepartmentType.Virtual },
  { label: '办事处', value: DepartmentType.Office },
  { label: '子公司', value: DepartmentType.Subsidiary },
  { label: '其他', value: DepartmentType.Other },
]

// ==================== 角色 ====================

export const ROLE_TYPE_OPTIONS = [
  { label: '系统', value: RoleType.System },
  { label: '业务', value: RoleType.Business },
  { label: '自定义', value: RoleType.Custom },
]

export const DATA_SCOPE_OPTIONS = [
  { label: '本人数据', value: DataPermissionScope.SelfOnly },
  { label: '本部门', value: DataPermissionScope.DepartmentOnly },
  { label: '本部门及下级', value: DataPermissionScope.DepartmentAndChildren },
  { label: '全部数据', value: DataPermissionScope.All },
  { label: '自定义', value: DataPermissionScope.Custom },
]

export const PERMISSION_ACTION_OPTIONS = [
  { label: '授予权限', value: PermissionAction.Grant },
  { label: '拒绝权限', value: PermissionAction.Deny },
]

// ==================== 租户 ====================

export const TENANT_ISOLATION_MODE_OPTIONS = [
  { label: '字段隔离', value: TenantIsolationMode.Field },
  { label: '数据库隔离', value: TenantIsolationMode.Database },
  { label: 'Schema隔离', value: TenantIsolationMode.Schema },
]

export const TENANT_DATABASE_TYPE_OPTIONS = [
  { label: 'SQL Server', value: TenantDatabaseType.SqlServer },
  { label: 'MySql', value: TenantDatabaseType.MySql },
  { label: 'PostgreSql', value: TenantDatabaseType.PostgreSql },
  { label: 'SQLite', value: TenantDatabaseType.SQLite },
  { label: 'Oracle', value: TenantDatabaseType.Oracle },
]

export const TENANT_STATUS_OPTIONS = [
  { label: '正常', value: TenantStatus.Normal },
  { label: '暂停', value: TenantStatus.Suspended },
  { label: '过期', value: TenantStatus.Expired },
  { label: '禁用', value: TenantStatus.Disabled },
]

export const TENANT_CONFIG_STATUS_OPTIONS = [
  { label: '待配置', value: TenantConfigStatus.Pending },
  { label: '配置中', value: TenantConfigStatus.Configuring },
  { label: '已配置', value: TenantConfigStatus.Configured },
  { label: '配置失败', value: TenantConfigStatus.Failed },
  { label: '已停用', value: TenantConfigStatus.Disabled },
]

export const MEMBER_TYPE_OPTIONS = [
  { label: '租户所有者', value: TenantMemberType.Owner },
  { label: '租户管理员', value: TenantMemberType.Admin },
  { label: '普通成员', value: TenantMemberType.Member },
  { label: '外部协作者', value: TenantMemberType.External },
  { label: '访客', value: TenantMemberType.Guest },
  { label: '顾问', value: TenantMemberType.Consultant },
  { label: '平台管理员', value: TenantMemberType.PlatformAdmin },
]

export const MEMBER_INVITE_STATUS_OPTIONS = [
  { label: '待接受', value: TenantMemberInviteStatus.Pending },
  { label: '已接受', value: TenantMemberInviteStatus.Accepted },
  { label: '已拒绝', value: TenantMemberInviteStatus.Rejected },
  { label: '已撤销', value: TenantMemberInviteStatus.Revoked },
  { label: '已过期', value: TenantMemberInviteStatus.Expired },
]

// ==================== 菜单类型 ====================

export const MENU_TYPE = {
  DIR: MenuType.Directory,
  MENU: MenuType.Menu,
  BUTTON: MenuType.Button,
} as const

// ==================== 文件 ====================

export const FILE_TYPE_OPTIONS = [
  { label: '图片', value: FileType.Image },
  { label: '文档', value: FileType.Document },
  { label: '视频', value: FileType.Video },
  { label: '音频', value: FileType.Audio },
  { label: '压缩包', value: FileType.Archive },
  { label: '其他', value: FileType.Other },
]

export const FILE_STATUS_OPTIONS = [
  { label: '正常', value: FileStatus.Normal },
  { label: '上传中', value: FileStatus.Uploading },
  { label: '上传失败', value: FileStatus.UploadFailed },
  { label: '处理中', value: FileStatus.Processing },
  { label: '已删除', value: FileStatus.Deleted },
  { label: '已归档', value: FileStatus.Archived },
  { label: '已过期', value: FileStatus.Expired },
  { label: '损坏', value: FileStatus.Corrupted },
  { label: '违规', value: FileStatus.Violation },
]

// ==================== 邮件 ====================

export const EMAIL_TYPE_OPTIONS = [
  { label: '系统邮件', value: EmailType.System },
  { label: '验证邮件', value: EmailType.Verification },
  { label: '通知邮件', value: EmailType.Notification },
  { label: '营销邮件', value: EmailType.Marketing },
  { label: '自定义邮件', value: EmailType.Custom },
]

export const EMAIL_STATUS_OPTIONS = [
  { label: '待发送', value: EmailStatus.Pending },
  { label: '发送中', value: EmailStatus.Sending },
  { label: '发送成功', value: EmailStatus.Success },
  { label: '发送失败', value: EmailStatus.Failed },
  { label: '已取消', value: EmailStatus.Cancelled },
]

// ==================== 短信 ====================

export const SMS_TYPE_OPTIONS = [
  { label: '验证码', value: SmsType.VerificationCode },
  { label: '通知短信', value: SmsType.Notification },
  { label: '营销短信', value: SmsType.Marketing },
  { label: '自定义短信', value: SmsType.Custom },
]

export const SMS_STATUS_OPTIONS = [
  { label: '待发送', value: SmsStatus.Pending },
  { label: '发送中', value: SmsStatus.Sending },
  { label: '发送成功', value: SmsStatus.Success },
  { label: '发送失败', value: SmsStatus.Failed },
  { label: '已取消', value: SmsStatus.Cancelled },
]

// ==================== 任务 ====================

export const TRIGGER_TYPE_OPTIONS = [
  { label: '立即执行', value: TriggerType.Immediate },
  { label: '定时执行', value: TriggerType.Schedule },
  { label: '循环执行', value: TriggerType.Recurring },
  { label: 'Cron 表达式', value: TriggerType.Cron },
]

export const RUN_TASK_STATUS_OPTIONS = [
  { label: '待执行', value: RunTaskStatus.Pending },
  { label: '执行中', value: RunTaskStatus.Running },
  { label: '执行成功', value: RunTaskStatus.Success },
  { label: '执行失败', value: RunTaskStatus.Failed },
  { label: '已停止', value: RunTaskStatus.Stopped },
  { label: '已暂停', value: RunTaskStatus.Paused },
]

// ==================== OAuth 应用 ====================

export const OAUTH_APP_TYPE_OPTIONS = [
  { label: 'Web 应用', value: OAuthAppType.Web },
  { label: '移动应用', value: OAuthAppType.Mobile },
  { label: '桌面应用', value: OAuthAppType.Desktop },
  { label: '服务应用', value: OAuthAppType.Service },
]

export const OPENAPI_SIGNATURE_ALGORITHM_OPTIONS = [
  { label: 'HMACSHA256', value: 'HMACSHA256' },
  { label: 'HMACSHA512', value: 'HMACSHA512' },
  { label: 'HMACSHA1', value: 'HMACSHA1' },
  { label: 'RSASHA256', value: 'RSASHA256' },
  { label: 'SM2', value: 'SM2' },
]

export const OPENAPI_CONTENT_SIGN_ALGORITHM_OPTIONS = [
  { label: 'SHA256', value: 'SHA256' },
  { label: 'SHA512', value: 'SHA512' },
  { label: 'MD5', value: 'MD5' },
]

export const OPENAPI_ENCRYPT_ALGORITHM_OPTIONS = [
  { label: 'AES-CBC', value: 'AES-CBC' },
  { label: 'BLOWFISH', value: 'BLOWFISH' },
  { label: 'NONE', value: 'NONE' },
]

// ==================== 审查 ====================

export const REVIEW_STATUS_OPTIONS = [
  { label: '待审查', value: AuditStatus.Pending },
  { label: '审查中', value: AuditStatus.InProgress },
  { label: '已通过', value: AuditStatus.Approved },
  { label: '已拒绝', value: AuditStatus.Rejected },
  { label: '已撤回', value: AuditStatus.Withdrawn },
]

export const REVIEW_RESULT_OPTIONS = [
  { label: '通过', value: AuditResult.Pass },
  { label: '拒绝', value: AuditResult.Reject },
  { label: '退回修改', value: AuditResult.Return },
]

// ==================== 会话/设备 ====================

export const DEVICE_TYPE_OPTIONS = [
  { label: '未知', value: DeviceType.Unknown },
  { label: 'Web浏览器', value: DeviceType.Web },
  { label: 'iOS移动端', value: DeviceType.iOS },
  { label: 'Android移动端', value: DeviceType.Android },
  { label: 'Windows桌面', value: DeviceType.Windows },
  { label: 'macOS桌面', value: DeviceType.macOS },
  { label: 'Linux桌面', value: DeviceType.Linux },
  { label: '平板设备', value: DeviceType.Tablet },
  { label: '小程序', value: DeviceType.MiniProgram },
  { label: 'API调用', value: DeviceType.Api },
]

/** [Flags] 枚举，value 为数字位标志 */
export const TWO_FACTOR_METHOD_OPTIONS = [
  { label: '未启用', value: TwoFactorMethod.None },
  { label: 'TOTP', value: TwoFactorMethod.Totp },
  { label: '邮箱验证码', value: TwoFactorMethod.Email },
  { label: '短信验证码', value: TwoFactorMethod.Phone },
]

export const STATISTICS_PERIOD_OPTIONS = [
  { label: '今日', value: StatisticsPeriod.Today },
  { label: '昨日', value: StatisticsPeriod.Yesterday },
  { label: '本周', value: StatisticsPeriod.ThisWeek },
  { label: '上周', value: StatisticsPeriod.LastWeek },
  { label: '本月', value: StatisticsPeriod.ThisMonth },
  { label: '上月', value: StatisticsPeriod.LastMonth },
  { label: '本年', value: StatisticsPeriod.ThisYear },
  { label: '去年', value: StatisticsPeriod.LastYear },
  { label: '自定义', value: StatisticsPeriod.Custom },
]

// ==================== 配置 ====================

export const CONFIG_TYPE_OPTIONS = [
  { label: '租户配置', value: ConfigType.Tenant },
  { label: '组织配置', value: ConfigType.Organization },
  { label: '用户配置', value: ConfigType.User },
  { label: '应用配置', value: ConfigType.Application },
  { label: '环境配置', value: ConfigType.Environment },
  { label: '功能配置', value: ConfigType.Feature },
]

export const CONFIG_DATA_TYPE_OPTIONS = [
  { label: '字符串', value: ConfigDataType.String },
  { label: '数字', value: ConfigDataType.Number },
  { label: '布尔值', value: ConfigDataType.Boolean },
  { label: 'JSON对象', value: ConfigDataType.Json },
  { label: '数组', value: ConfigDataType.Array },
]

// ==================== 权限 ====================

export const PERMISSION_TYPE_OPTIONS = [
  { label: '资源操作', value: PermissionType.ResourceBased },
  { label: '功能', value: PermissionType.Functional },
  { label: '数据范围', value: PermissionType.DataScope },
]

// ==================== 资源 ====================

export const RESOURCE_TYPE_OPTIONS = [
  { label: 'API', value: ResourceType.Api },
  { label: '文件', value: ResourceType.File },
  { label: '数据表', value: ResourceType.DataTable },
  { label: '业务对象', value: ResourceType.BusinessObject },
  { label: '其他', value: ResourceType.Other },
]

export const RESOURCE_ACCESS_LEVEL_OPTIONS = [
  { label: '匿名访问', value: ResourceAccessLevel.Public },
  { label: '仅需认证', value: ResourceAccessLevel.Authenticated },
  { label: '需要授权', value: ResourceAccessLevel.Authorized },
]

// ==================== 操作 ====================

export const HTTP_METHOD_OPTIONS = [
  { label: 'GET', value: HttpMethodType.GET },
  { label: 'POST', value: HttpMethodType.POST },
  { label: 'PUT', value: HttpMethodType.PUT },
  { label: 'DELETE', value: HttpMethodType.DELETE },
  { label: 'PATCH', value: HttpMethodType.PATCH },
  { label: 'HEAD', value: HttpMethodType.HEAD },
  { label: 'OPTIONS', value: HttpMethodType.OPTIONS },
  { label: '所有方法', value: HttpMethodType.ALL },
]

export const OPERATION_CATEGORY_OPTIONS = [
  { label: 'CRUD', value: OperationCategory.Crud },
  { label: '业务', value: OperationCategory.Business },
  { label: '管理', value: OperationCategory.Admin },
  { label: '系统', value: OperationCategory.System },
  { label: '自定义', value: OperationCategory.Custom },
]

export const OPERATION_TYPE_OPTIONS = [
  { label: '创建', value: OperationTypeCode.Create },
  { label: '读取', value: OperationTypeCode.Read },
  { label: '更新', value: OperationTypeCode.Update },
  { label: '删除', value: OperationTypeCode.Delete },
  { label: '查看详情', value: OperationTypeCode.View },
  { label: '审批', value: OperationTypeCode.Approve },
  { label: '执行', value: OperationTypeCode.Execute },
  { label: '导入', value: OperationTypeCode.Import },
  { label: '导出', value: OperationTypeCode.Export },
  { label: '上传', value: OperationTypeCode.Upload },
  { label: '下载', value: OperationTypeCode.Download },
  { label: '打印', value: OperationTypeCode.Print },
  { label: '分享', value: OperationTypeCode.Share },
  { label: '授权', value: OperationTypeCode.Grant },
  { label: '撤销', value: OperationTypeCode.Revoke },
  { label: '启用', value: OperationTypeCode.Enable },
  { label: '禁用', value: OperationTypeCode.Disable },
  { label: '自定义', value: OperationTypeCode.Custom },
]

// ==================== 权限条件 ====================

export const CONDITION_OPERATOR_OPTIONS = [
  { label: '等于', value: ConditionOperator.Equals },
  { label: '不等于', value: ConditionOperator.NotEquals },
  { label: '大于', value: ConditionOperator.GreaterThan },
  { label: '大于等于', value: ConditionOperator.GreaterThanOrEquals },
  { label: '小于', value: ConditionOperator.LessThan },
  { label: '小于等于', value: ConditionOperator.LessThanOrEquals },
  { label: '包含', value: ConditionOperator.Contains },
  { label: '不包含', value: ConditionOperator.NotContains },
  { label: '在集合中', value: ConditionOperator.In },
  { label: '不在集合中', value: ConditionOperator.NotIn },
  { label: '在范围内', value: ConditionOperator.Between },
  { label: '以…开头', value: ConditionOperator.StartsWith },
  { label: '以…结尾', value: ConditionOperator.EndsWith },
  { label: '为空', value: ConditionOperator.IsNull },
  { label: '不为空', value: ConditionOperator.IsNotNull },
]

// ==================== 权限委托 ====================

export const DELEGATION_STATUS_OPTIONS = [
  { label: '待生效', value: DelegationStatus.Pending },
  { label: '生效中', value: DelegationStatus.Active },
  { label: '已过期', value: DelegationStatus.Expired },
  { label: '已撤销', value: DelegationStatus.Revoked },
]

export const PERMISSION_REQUEST_STATUS_OPTIONS = [
  { label: '待审批', value: PermissionRequestStatus.Pending },
  { label: '已批准', value: PermissionRequestStatus.Approved },
  { label: '已拒绝', value: PermissionRequestStatus.Rejected },
  { label: '已撤回', value: PermissionRequestStatus.Withdrawn },
  { label: '已过期', value: PermissionRequestStatus.Expired },
]

// ==================== 字段级安全 ====================

export const FIELD_MASK_STRATEGY_OPTIONS = [
  { label: '不脱敏', value: FieldMaskStrategy.None },
  { label: '完全隐藏', value: FieldMaskStrategy.Hidden },
  { label: '全部星号', value: FieldMaskStrategy.FullMask },
  { label: '部分脱敏', value: FieldMaskStrategy.PartialMask },
  { label: '哈希', value: FieldMaskStrategy.Hash },
  { label: '固定替换', value: FieldMaskStrategy.Redact },
  { label: '自定义', value: FieldMaskStrategy.Custom },
]

export const FIELD_SECURITY_TARGET_TYPE_OPTIONS = [
  { label: '角色', value: FieldSecurityTargetType.Role },
  { label: '用户', value: FieldSecurityTargetType.User },
  { label: '权限', value: FieldSecurityTargetType.Permission },
  { label: '部门', value: FieldSecurityTargetType.Department },
]

// ==================== 权限变更记录 ====================

export const PERMISSION_CHANGE_TYPE_OPTIONS = [
  { label: '角色授予权限', value: PermissionChangeType.RoleGrantPermission },
  { label: '角色撤销权限', value: PermissionChangeType.RoleRevokePermission },
  { label: '用户直授权限', value: PermissionChangeType.UserGrantPermission },
  { label: '用户撤销直授权限', value: PermissionChangeType.UserRevokePermission },
  { label: '用户分配角色', value: PermissionChangeType.UserAssignRole },
  { label: '用户移除角色', value: PermissionChangeType.UserRemoveRole },
  { label: '用户直授权限拒绝', value: PermissionChangeType.UserDenyPermission },
  { label: '角色权限拒绝', value: PermissionChangeType.RoleDenyPermission },
  { label: '用户获得委托授权', value: PermissionChangeType.UserDelegateGrant },
  { label: '用户委托授权收回', value: PermissionChangeType.UserDelegateRevoke },
]

// ==================== 约束规则 ====================

export const CONSTRAINT_TYPE_OPTIONS = [
  { label: '静态职责分离', value: ConstraintType.SSD },
  { label: '动态职责分离', value: ConstraintType.DSD },
  { label: '互斥约束', value: ConstraintType.MutualExclusion },
  { label: '基数约束', value: ConstraintType.Cardinality },
  { label: '先决条件', value: ConstraintType.Prerequisite },
  { label: '时间约束', value: ConstraintType.Temporal },
  { label: '位置约束', value: ConstraintType.Location },
  { label: '自定义约束', value: ConstraintType.Custom },
]

export const VIOLATION_ACTION_OPTIONS = [
  { label: '拒绝', value: ViolationAction.Deny },
  { label: '警告放行', value: ViolationAction.Warning },
  { label: '仅记录日志', value: ViolationAction.Log },
  { label: '需要审批', value: ViolationAction.RequireApproval },
]

// ==================== 通知 ====================

export const NOTIFICATION_TYPE_OPTIONS = [
  { label: '系统公告', value: NotificationType.System },
  { label: '安全通知', value: NotificationType.Security },
  { label: '业务通知', value: NotificationType.Business },
  { label: '待办通知', value: NotificationType.Todo },
  { label: '紧急通知', value: NotificationType.Emergency },
]

export const NOTIFICATION_STATUS_OPTIONS = [
  { label: '未读', value: NotificationStatus.Unread },
  { label: '已读', value: NotificationStatus.Read },
  { label: '已删除', value: NotificationStatus.Deleted },
]
