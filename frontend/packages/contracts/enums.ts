/*
 * Copyright (c) 2021-Present XiHanFun and contributors.
 * Licensed under the MIT License. See LICENSE in the project root for license information.
 */

// 由 scripts/gen-contracts.mjs 从 contracts/openapi.json 生成；不要手改，改后端契约后执行 pnpm gen:contracts

// 枚举文案由后端消息目录按语言提供（随 N1 的 metadata.enums.list 接入）；这里只有成员与类型

export const AccessPolicyAction = ['LogOnly', 'Deny', 'Allow'] as const
export type AccessPolicyAction = (typeof AccessPolicyAction)[number]

export const AccessPolicyStatus = ['Active', 'Disabled'] as const
export type AccessPolicyStatus = (typeof AccessPolicyStatus)[number]

export const AccessResult = ['Success', 'Failed', 'Forbidden', 'Unauthorized', 'NotFound', 'ServerError'] as const
export type AccessResult = (typeof AccessResult)[number]

export const AiStatus = ['Active', 'Disabled'] as const
export type AiStatus = (typeof AiStatus)[number]

export const Architecture = ['X86', 'X64', 'Arm', 'Arm64', 'Wasm', 'S390x', 'LoongArch64', 'Armv6', 'Ppc64le', 'RiscV64'] as const
export type Architecture = (typeof Architecture)[number]

export const BotProvider = ['DingTalk', 'Lark', 'WeCom'] as const
export type BotProvider = (typeof BotProvider)[number]

export const CacheLayer = ['Store', 'Objects'] as const
export type CacheLayer = (typeof CacheLayer)[number]

export const ChatCallEndReason = ['Completed', 'Cancelled', 'Rejected', 'Missed', 'Interrupted'] as const
export type ChatCallEndReason = (typeof ChatCallEndReason)[number]

export const ChatCallMedia = ['Voice', 'Video'] as const
export type ChatCallMedia = (typeof ChatCallMedia)[number]

export const ChatCallParticipantStatus = ['Invited', 'Joined', 'Rejected', 'Missed', 'Left'] as const
export type ChatCallParticipantStatus = (typeof ChatCallParticipantStatus)[number]

export const ChatCallStatus = ['Ringing', 'Ongoing', 'Ended'] as const
export type ChatCallStatus = (typeof ChatCallStatus)[number]

export const ChatConversationType = ['Single', 'Group', 'Department', 'Assistant'] as const
export type ChatConversationType = (typeof ChatConversationType)[number]

export const ChatMemberRole = ['Owner', 'Admin', 'Member'] as const
export type ChatMemberRole = (typeof ChatMemberRole)[number]

export const ChatMessageType = ['Text', 'Image', 'Voice', 'File', 'Assistant', 'Call', 'Record', 'System'] as const
export type ChatMessageType = (typeof ChatMessageType)[number]

export const CodeGenDatabaseType = ['MySql', 'SqlServer', 'PostgreSql', 'Oracle', 'Sqlite'] as const
export type CodeGenDatabaseType = (typeof CodeGenDatabaseType)[number]

export const CodeGenDeliveryType = ['Zip', 'Project'] as const
export type CodeGenDeliveryType = (typeof CodeGenDeliveryType)[number]

export const CodeGenDictSelectorType = ['DictSelector', 'EnumSelector', 'ConstSelector', 'TableSelector', 'TreeSelector'] as const
export type CodeGenDictSelectorType = (typeof CodeGenDictSelectorType)[number]

export const CodeGenGenerationStatus = ['NotGenerated', 'Generated', 'Failed'] as const
export type CodeGenGenerationStatus = (typeof CodeGenGenerationStatus)[number]

export const CodeGenHtmlType = ['Input', 'Textarea', 'Select', 'Radio', 'Checkbox', 'DatePicker', 'DateTimePicker', 'TimePicker', 'ImageUpload', 'FileUpload', 'Editor', 'InputNumber', 'Switch', 'TreeSelect'] as const
export type CodeGenHtmlType = (typeof CodeGenHtmlType)[number]

export const CodeGenQueryType = ['Equal', 'NotEqual', 'GreaterThan', 'GreaterThanOrEqual', 'LessThan', 'LessThanOrEqual', 'Like', 'LikeLeft', 'LikeRight', 'Between', 'In', 'NotIn'] as const
export type CodeGenQueryType = (typeof CodeGenQueryType)[number]

export const CodeGenScope = ['All', 'BackendOnly', 'FrontendOnly'] as const
export type CodeGenScope = (typeof CodeGenScope)[number]

export const CodeGenSide = ['Backend', 'Frontend', 'Registration'] as const
export type CodeGenSide = (typeof CodeGenSide)[number]

export const CodeGenStatus = ['Active', 'Disabled'] as const
export type CodeGenStatus = (typeof CodeGenStatus)[number]

export const CodeGenTarget = ['NewPackage', 'ExistingPackage'] as const
export type CodeGenTarget = (typeof CodeGenTarget)[number]

export const CodeGenTemplateEngine = ['Scriban'] as const
export type CodeGenTemplateEngine = (typeof CodeGenTemplateEngine)[number]

export const CodeGenTemplateType = ['Single', 'Tree', 'MasterDetail', 'Universal'] as const
export type CodeGenTemplateType = (typeof CodeGenTemplateType)[number]

export const CodeGenWriteMode = ['AlwaysOverwrite', 'WriteOnce'] as const
export type CodeGenWriteMode = (typeof CodeGenWriteMode)[number]

export const ConditionDiagnostic = ['UnregisteredAttribute', 'LegacyName', 'InvalidCondition', 'BindingInactive'] as const
export type ConditionDiagnostic = (typeof ConditionDiagnostic)[number]

export const ConditionGroupConclusion = ['Evaluable', 'NeverMatches', 'BindingInactive', 'Disabled'] as const
export type ConditionGroupConclusion = (typeof ConditionGroupConclusion)[number]

export const ConditionHitAction = ['LogOnly', 'Deny', 'Allow'] as const
export type ConditionHitAction = (typeof ConditionHitAction)[number]

export const ConditionHitPhase = ['BeforeAuthentication', 'AfterAuthentication', 'Login', 'Authorization'] as const
export type ConditionHitPhase = (typeof ConditionHitPhase)[number]

export const ConditionHitSource = ['AccessPolicy', 'PermissionCondition'] as const
export type ConditionHitSource = (typeof ConditionHitSource)[number]

export const ConditionOperator = ['Equals', 'NotEquals', 'GreaterThan', 'GreaterThanOrEquals', 'LessThan', 'LessThanOrEquals', 'Contains', 'NotContains', 'In', 'NotIn', 'Between', 'StartsWith', 'EndsWith', 'IsNull', 'IsNotNull'] as const
export type ConditionOperator = (typeof ConditionOperator)[number]

export const ConditionOutcome = ['Matched', 'NotMatched', 'Indeterminate', 'Pending'] as const
export type ConditionOutcome = (typeof ConditionOutcome)[number]

export const ConditionStatus = ['Active', 'Disabled'] as const
export type ConditionStatus = (typeof ConditionStatus)[number]

export const ConditionValueKind = ['String', 'Number', 'Boolean', 'DateTime', 'Date', 'TimeOfDay', 'IpAddress'] as const
export type ConditionValueKind = (typeof ConditionValueKind)[number]

export const ConditionVerdict = ['NotGranted', 'Unconditional', 'Granted', 'Denied'] as const
export type ConditionVerdict = (typeof ConditionVerdict)[number]

export const ConfigDataType = ['Text', 'Number', 'Boolean', 'Json', 'Array'] as const
export type ConfigDataType = (typeof ConfigDataType)[number]

export const ConstraintRuleStatus = ['Active', 'Disabled'] as const
export type ConstraintRuleStatus = (typeof ConstraintRuleStatus)[number]

export const ConstraintTargetType = ['Role', 'Permission', 'User'] as const
export type ConstraintTargetType = (typeof ConstraintTargetType)[number]

export const ConstraintType = ['Ssd', 'Dsd', 'MutualExclusion', 'Cardinality', 'Condition'] as const
export type ConstraintType = (typeof ConstraintType)[number]

export const ConstraintViolationAction = ['Deny', 'Warning', 'Log', 'RequireApproval'] as const
export type ConstraintViolationAction = (typeof ConstraintViolationAction)[number]

export const ContactChannel = ['Email', 'Phone'] as const
export type ContactChannel = (typeof ContactChannel)[number]

export const DataScope = ['SelfOnly', 'DepartmentOnly', 'DepartmentAndChildren', 'All', 'Custom'] as const
export type DataScope = (typeof DataScope)[number]

export const DelegationStatus = ['Pending', 'Active', 'Expired', 'Revoked'] as const
export type DelegationStatus = (typeof DelegationStatus)[number]

export const DeliveryChannel = ['InApp', 'Email', 'Sms', 'Bot'] as const
export type DeliveryChannel = (typeof DeliveryChannel)[number]

export const DepartmentType = ['Department', 'Corporation', 'Headquarters', 'Company', 'Branch', 'Division', 'Center', 'Section', 'Team', 'Group', 'Project', 'Workgroup', 'Virtual', 'Office', 'Subsidiary', 'Other'] as const
export type DepartmentType = (typeof DepartmentType)[number]

export const DiffKind = ['Insert', 'Update', 'Delete', 'Restore'] as const
export type DiffKind = (typeof DiffKind)[number]

export const DriveType = ['Unknown', 'NoRootDirectory', 'Removable', 'Fixed', 'Network', 'CDRom', 'Ram'] as const
export type DriveType = (typeof DriveType)[number]

export const EditionStatus = ['Active', 'Disabled'] as const
export type EditionStatus = (typeof EditionStatus)[number]

export const ExportFormat = ['Csv', 'Xlsx'] as const
export type ExportFormat = (typeof ExportFormat)[number]

export const ExportScope = ['CurrentPage', 'SearchResult', 'All'] as const
export type ExportScope = (typeof ExportScope)[number]

export const ExportTaskStatus = ['Pending', 'Processing', 'Success', 'Failed'] as const
export type ExportTaskStatus = (typeof ExportTaskStatus)[number]

export const FieldMaskStrategy = ['None', 'Hidden', 'FullMask', 'PartialMask', 'Hash', 'Redact'] as const
export type FieldMaskStrategy = (typeof FieldMaskStrategy)[number]

export const FieldRuleStatus = ['Active', 'Disabled'] as const
export type FieldRuleStatus = (typeof FieldRuleStatus)[number]

export const FieldSecurityTargetKind = ['Role', 'User', 'Department'] as const
export type FieldSecurityTargetKind = (typeof FieldSecurityTargetKind)[number]

export const FileAccessLevel = ['Authenticated', 'Authorized', 'Public'] as const
export type FileAccessLevel = (typeof FileAccessLevel)[number]

export const FileKind = ['Image', 'Document', 'Video', 'Audio', 'Archive', 'Other'] as const
export type FileKind = (typeof FileKind)[number]

export const FileStatus = ['Normal', 'Archived', 'Corrupted'] as const
export type FileStatus = (typeof FileStatus)[number]

export const FileStorageStatus = ['Normal', 'VerificationFailed', 'Unavailable'] as const
export type FileStorageStatus = (typeof FileStorageStatus)[number]

export const FileUploadStatus = ['Uploading', 'Completed', 'Aborted', 'Expired', 'Failed'] as const
export type FileUploadStatus = (typeof FileUploadStatus)[number]

export const GrantSubjectKind = ['Role', 'User'] as const
export type GrantSubjectKind = (typeof GrantSubjectKind)[number]

export const JobRetryBackoff = ['Fixed', 'Exponential'] as const
export type JobRetryBackoff = (typeof JobRetryBackoff)[number]

export const JobRunState = ['Running', 'Succeeded', 'Failed', 'Canceled', 'Lost'] as const
export type JobRunState = (typeof JobRunState)[number]

export const JobScheduleKind = ['Manual', 'Interval', 'Cron', 'Once'] as const
export type JobScheduleKind = (typeof JobScheduleKind)[number]

export const JobSource = ['Code', 'Runtime', 'Overridden'] as const
export type JobSource = (typeof JobSource)[number]

export const JobTrigger = ['Schedule', 'Manual', 'Recovery', 'Retry'] as const
export type JobTrigger = (typeof JobTrigger)[number]

export const KnowledgeIndexStatus = ['Pending', 'Indexed', 'Failed'] as const
export type KnowledgeIndexStatus = (typeof KnowledgeIndexStatus)[number]

export const KnowledgeSourceType = ['PasteText', 'UploadFile'] as const
export type KnowledgeSourceType = (typeof KnowledgeSourceType)[number]

export const LifecycleState = ['Declared', 'Resolved', 'Configured', 'Started', 'Ready', 'Degraded', 'Draining', 'Stopped', 'Failed'] as const
export type LifecycleState = (typeof LifecycleState)[number]

export const LoginEvent = ['Login', 'Refresh', 'Logout', 'SwitchTenant', 'ImpersonationStart', 'ImpersonationStop', 'ChangePassword', 'TwoFactorChallenge', 'TwoFactorEnable', 'TwoFactorDisable', 'TwoFactorReset', 'RoleActivation'] as const
export type LoginEvent = (typeof LoginEvent)[number]

export const MenuKind = ['Page', 'Directory', 'Link'] as const
export type MenuKind = (typeof MenuKind)[number]

export const MenuStatus = ['Active', 'Disabled'] as const
export type MenuStatus = (typeof MenuStatus)[number]

export const MessageSendStatus = ['Pending', 'Sending', 'Success', 'Failed', 'Cancelled'] as const
export type MessageSendStatus = (typeof MessageSendStatus)[number]

export const NetworkInterfaceType = ['Unknown', 'Ethernet', 'TokenRing', 'Fddi', 'BasicIsdn', 'PrimaryIsdn', 'Ppp', 'Loopback', 'Ethernet3Megabit', 'Slip', 'Atm', 'GenericModem', 'FastEthernetT', 'Isdn', 'FastEthernetFx', 'Wireless80211', 'AsymmetricDsl', 'RateAdaptDsl', 'SymmetricDsl', 'VeryHighSpeedDsl', 'IPOverAtm', 'GigabitEthernet', 'Tunnel', 'MultiRateSymmetricDsl', 'HighPerformanceSerialBus', 'Wman', 'Wwanpp', 'Wwanpp2'] as const
export type NetworkInterfaceType = (typeof NetworkInterfaceType)[number]

export const NotificationContentFormat = ['Text', 'Markdown', 'Html'] as const
export type NotificationContentFormat = (typeof NotificationContentFormat)[number]

export const NotificationPriority = ['Low', 'Normal', 'High', 'Urgent'] as const
export type NotificationPriority = (typeof NotificationPriority)[number]

export const NotificationRecipientFilter = ['All', 'Unread', 'Read', 'Unconfirmed'] as const
export type NotificationRecipientFilter = (typeof NotificationRecipientFilter)[number]

export const NotificationTargetType = ['All', 'Role', 'Department', 'User'] as const
export type NotificationTargetType = (typeof NotificationTargetType)[number]

export const NotificationType = ['System', 'Security', 'Business', 'Todo', 'Emergency'] as const
export type NotificationType = (typeof NotificationType)[number]

export const NumberingDateFormat = ['None', 'Yyyy', 'YyyyMM', 'YyyyMMdd', 'YyMM', 'YyMMdd', 'MMdd'] as const
export type NumberingDateFormat = (typeof NumberingDateFormat)[number]

export const NumberingResetCycle = ['Never', 'Daily', 'Monthly', 'Yearly'] as const
export type NumberingResetCycle = (typeof NumberingResetCycle)[number]

export const NumberingScope = ['Auto', 'Tenant', 'Platform'] as const
export type NumberingScope = (typeof NumberingScope)[number]

export const OAuthAppType = ['Web', 'Mobile', 'Desktop', 'Service'] as const
export type OAuthAppType = (typeof OAuthAppType)[number]

export const OAuthClientType = ['Confidential', 'Public'] as const
export type OAuthClientType = (typeof OAuthClientType)[number]

export const OperationalStatus = ['Up', 'Down', 'Testing', 'Unknown', 'Dormant', 'NotPresent', 'LowerLayerDown'] as const
export type OperationalStatus = (typeof OperationalStatus)[number]

export const OrganizationStatus = ['Active', 'Disabled'] as const
export type OrganizationStatus = (typeof OrganizationStatus)[number]

export const PermissionAction = ['Grant', 'Deny'] as const
export type PermissionAction = (typeof PermissionAction)[number]

export const PermissionChangeType = ['RoleGrant', 'RoleRevoke', 'RoleAssign', 'RoleUnassign', 'RoleDataScope', 'RoleDelete', 'EditionPermissions', 'TenantEdition', 'RoleDeny', 'RoleAssignmentWindow', 'UserGrant', 'UserDeny', 'UserRevoke', 'UserDataScope', 'FieldSecurity', 'RoleInheritance', 'Delegation', 'ConditionChange', 'ConstraintViolation', 'TenantOwner'] as const
export type PermissionChangeType = (typeof PermissionChangeType)[number]

export const PermissionRequestStatus = ['Pending', 'Approved', 'Rejected', 'Withdrawn'] as const
export type PermissionRequestStatus = (typeof PermissionRequestStatus)[number]

export const PermissionScope = ['Platform', 'Tenant', 'Both'] as const
export type PermissionScope = (typeof PermissionScope)[number]

export const PrintTemplateScope = ['Auto', 'Tenant', 'Global'] as const
export type PrintTemplateScope = (typeof PrintTemplateScope)[number]

export const PrintTemplateStatus = ['Active', 'Disabled'] as const
export type PrintTemplateStatus = (typeof PrintTemplateStatus)[number]

export const RoleStatus = ['Active', 'Disabled'] as const
export type RoleStatus = (typeof RoleStatus)[number]

export const SessionRoleStatus = ['Active', 'Inactive', 'Expired'] as const
export type SessionRoleStatus = (typeof SessionRoleStatus)[number]

export const SettingStatus = ['Active', 'Disabled'] as const
export type SettingStatus = (typeof SettingStatus)[number]

export const SmsProvider = ['Aliyun', 'Tencent'] as const
export type SmsProvider = (typeof SmsProvider)[number]

export const StorageConfigStatus = ['Active', 'Disabled'] as const
export type StorageConfigStatus = (typeof StorageConfigStatus)[number]

export const StorageType = ['Local', 'S3', 'Oss', 'Cos', 'MinIO'] as const
export type StorageType = (typeof StorageType)[number]

export const TemplateChannel = ['InApp', 'Email', 'Sms', 'Bot'] as const
export type TemplateChannel = (typeof TemplateChannel)[number]

export const TenantDatabaseStatus = ['NotRequired', 'Pending', 'Ready', 'Failed'] as const
export type TenantDatabaseStatus = (typeof TenantDatabaseStatus)[number]

export const TenantIsolationMode = ['Field', 'Database'] as const
export type TenantIsolationMode = (typeof TenantIsolationMode)[number]

export const TenantMemberType = ['Member', 'Support'] as const
export type TenantMemberType = (typeof TenantMemberType)[number]

export const TenantStatus = ['Active', 'Suspended', 'Expired'] as const
export type TenantStatus = (typeof TenantStatus)[number]

export const TraceDimension = ['TraceId', 'UserId', 'UserName', 'SessionId', 'ClientIp'] as const
export type TraceDimension = (typeof TraceDimension)[number]

export const TraceItemStatus = ['Success', 'Info', 'Warning', 'Failure'] as const
export type TraceItemStatus = (typeof TraceItemStatus)[number]

export const TraceLogType = ['Access', 'OpenApi', 'Operation', 'Login', 'Exception', 'Diff', 'PermissionChange', 'ConditionHit'] as const
export type TraceLogType = (typeof TraceLogType)[number]

export const TriggerState = ['Waiting', 'Paused', 'Acquired', 'Blocked', 'Complete', 'Error'] as const
export type TriggerState = (typeof TriggerState)[number]

export const TwoFactorMethod = ['Totp', 'Email', 'Phone'] as const
export type TwoFactorMethod = (typeof TwoFactorMethod)[number]

export const UserGender = ['Unknown', 'Male', 'Female'] as const
export type UserGender = (typeof UserGender)[number]

export const UserNotificationStatus = ['Unread', 'Read'] as const
export type UserNotificationStatus = (typeof UserNotificationStatus)[number]

export const UserSettingScene = ['Preference', 'Page'] as const
export type UserSettingScene = (typeof UserSettingScene)[number]

export const UserStatus = ['Active', 'Disabled', 'Locked'] as const
export type UserStatus = (typeof UserStatus)[number]

export const WorkbenchWidgetKind = ['Metric', 'Chart', 'List'] as const
export type WorkbenchWidgetKind = (typeof WorkbenchWidgetKind)[number]

export const WorkflowApprovalTimelineKind = ['Started', 'Completed', 'Transferred', 'AddedAssignees', 'CarbonCopied', 'AutoDecided', 'Pending', 'Finished'] as const
export type WorkflowApprovalTimelineKind = (typeof WorkflowApprovalTimelineKind)[number]

export const WorkflowBusinessStatus = ['Running', 'Suspended', 'Completed', 'Canceled', 'Faulted', 'Terminated'] as const
export type WorkflowBusinessStatus = (typeof WorkflowBusinessStatus)[number]

export const WorkflowDefinitionStatus = ['Draft', 'Published', 'Disabled', 'Archived'] as const
export type WorkflowDefinitionStatus = (typeof WorkflowDefinitionStatus)[number]

export const WorkflowInstanceStatus = ['Running', 'Suspended', 'Completed', 'Canceled', 'Faulted', 'Terminated'] as const
export type WorkflowInstanceStatus = (typeof WorkflowInstanceStatus)[number]

export const WorkflowNodeInstanceStatus = ['Running', 'Suspended', 'Completed', 'Canceled', 'Faulted', 'Compensated'] as const
export type WorkflowNodeInstanceStatus = (typeof WorkflowNodeInstanceStatus)[number]

export const WorkflowTaskRecordAction = ['Completed', 'Transferred', 'AddedAssignees', 'CarbonCopied'] as const
export type WorkflowTaskRecordAction = (typeof WorkflowTaskRecordAction)[number]

/** 全部枚举：类型名 → 成员 */
export const enums = {
  AccessPolicyAction,
  AccessPolicyStatus,
  AccessResult,
  AiStatus,
  Architecture,
  BotProvider,
  CacheLayer,
  ChatCallEndReason,
  ChatCallMedia,
  ChatCallParticipantStatus,
  ChatCallStatus,
  ChatConversationType,
  ChatMemberRole,
  ChatMessageType,
  CodeGenDatabaseType,
  CodeGenDeliveryType,
  CodeGenDictSelectorType,
  CodeGenGenerationStatus,
  CodeGenHtmlType,
  CodeGenQueryType,
  CodeGenScope,
  CodeGenSide,
  CodeGenStatus,
  CodeGenTarget,
  CodeGenTemplateEngine,
  CodeGenTemplateType,
  CodeGenWriteMode,
  ConditionDiagnostic,
  ConditionGroupConclusion,
  ConditionHitAction,
  ConditionHitPhase,
  ConditionHitSource,
  ConditionOperator,
  ConditionOutcome,
  ConditionStatus,
  ConditionValueKind,
  ConditionVerdict,
  ConfigDataType,
  ConstraintRuleStatus,
  ConstraintTargetType,
  ConstraintType,
  ConstraintViolationAction,
  ContactChannel,
  DataScope,
  DelegationStatus,
  DeliveryChannel,
  DepartmentType,
  DiffKind,
  DriveType,
  EditionStatus,
  ExportFormat,
  ExportScope,
  ExportTaskStatus,
  FieldMaskStrategy,
  FieldRuleStatus,
  FieldSecurityTargetKind,
  FileAccessLevel,
  FileKind,
  FileStatus,
  FileStorageStatus,
  FileUploadStatus,
  GrantSubjectKind,
  JobRetryBackoff,
  JobRunState,
  JobScheduleKind,
  JobSource,
  JobTrigger,
  KnowledgeIndexStatus,
  KnowledgeSourceType,
  LifecycleState,
  LoginEvent,
  MenuKind,
  MenuStatus,
  MessageSendStatus,
  NetworkInterfaceType,
  NotificationContentFormat,
  NotificationPriority,
  NotificationRecipientFilter,
  NotificationTargetType,
  NotificationType,
  NumberingDateFormat,
  NumberingResetCycle,
  NumberingScope,
  OAuthAppType,
  OAuthClientType,
  OperationalStatus,
  OrganizationStatus,
  PermissionAction,
  PermissionChangeType,
  PermissionRequestStatus,
  PermissionScope,
  PrintTemplateScope,
  PrintTemplateStatus,
  RoleStatus,
  SessionRoleStatus,
  SettingStatus,
  SmsProvider,
  StorageConfigStatus,
  StorageType,
  TemplateChannel,
  TenantDatabaseStatus,
  TenantIsolationMode,
  TenantMemberType,
  TenantStatus,
  TraceDimension,
  TraceItemStatus,
  TraceLogType,
  TriggerState,
  TwoFactorMethod,
  UserGender,
  UserNotificationStatus,
  UserSettingScene,
  UserStatus,
  WorkbenchWidgetKind,
  WorkflowApprovalTimelineKind,
  WorkflowBusinessStatus,
  WorkflowDefinitionStatus,
  WorkflowInstanceStatus,
  WorkflowNodeInstanceStatus,
  WorkflowTaskRecordAction,
} as const

export type EnumName = keyof typeof enums

/** 枚举成员的显示文案；后端枚举文案（metadata.enums.list）接入前原样返回成员名，未知成员也原样返回 */
export function enumLabel(_type: EnumName, value: string): string {
  return value
}
