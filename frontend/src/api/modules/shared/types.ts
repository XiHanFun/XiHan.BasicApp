/** 与后端 JsonStringEnumConverter 序列化值一致 */
export enum EnableStatus {
  Disabled = 'Disabled',
  Enabled = 'Enabled',
}

/** 与后端 JsonStringEnumConverter 序列化值一致 */
export enum ValidityStatus {
  Invalid = 'Invalid',
  Valid = 'Valid',
}

/** 与后端 JsonStringEnumConverter 序列化值一致 */
export enum PermissionType {
  ResourceBased = 'ResourceBased',
  Functional = 'Functional',
  DataScope = 'DataScope',
}

/** 权限作用侧：在平台（0 号租户）还是业务租户里生效；与后端 JsonStringEnumConverter 序列化值一致 */
export enum PermissionSide {
  Platform = 'Platform',
  Tenant = 'Tenant',
  Both = 'Both',
}
