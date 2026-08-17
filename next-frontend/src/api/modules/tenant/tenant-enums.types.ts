/** 与后端 JsonStringEnumConverter 序列化值一致 */
export enum TenantConfigStatus {
  Pending = 'Pending',
  Configuring = 'Configuring',
  Configured = 'Configured',
  Failed = 'Failed',
  Disabled = 'Disabled',
}

/** 与后端 JsonStringEnumConverter 序列化值一致 */
export enum TenantDatabaseType {
  SqlServer = 'SqlServer',
  MySql = 'MySql',
  PostgreSql = 'PostgreSql',
  SQLite = 'SQLite',
  Oracle = 'Oracle',
}

/** 与后端 JsonStringEnumConverter 序列化值一致 */
export enum TenantIsolationMode {
  Field = 'Field',
  Database = 'Database',
  Schema = 'Schema',
}

/** 与后端 JsonStringEnumConverter 序列化值一致 */
export enum TenantStatus {
  Normal = 'Normal',
  Suspended = 'Suspended',
  Expired = 'Expired',
  Disabled = 'Disabled',
}

/** 与后端 JsonStringEnumConverter 序列化值一致 */
export enum TenantMemberInviteStatus {
  Pending = 'Pending',
  Accepted = 'Accepted',
  Rejected = 'Rejected',
  Revoked = 'Revoked',
  Expired = 'Expired',
}

// 成员类型契约枚举已下沉到 packages/types/enums（供 _core 控制中心 / 个人中心「我的租户」复用），
// 此处反向 re-export 保持 `@/api` 入口不变（先例：src/api/modules/messaging/notification.types.ts）
export { TenantMemberType } from '~/types/enums'
