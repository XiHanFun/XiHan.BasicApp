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

/** 与后端 JsonStringEnumConverter 序列化值一致 */
export enum TenantMemberType {
  Owner = 'Owner',
  Admin = 'Admin',
  Member = 'Member',
  External = 'External',
  Guest = 'Guest',
  Consultant = 'Consultant',
  PlatformAdmin = 'PlatformAdmin',
}
